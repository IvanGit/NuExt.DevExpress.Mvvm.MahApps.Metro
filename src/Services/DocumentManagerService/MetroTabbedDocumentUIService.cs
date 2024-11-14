using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.UI.Interactivity;
using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DevExpress.Mvvm.UI
{
    /// <summary>
    /// The MetroTabbedDocumentUIService class is responsible for managing tabbed documents within a UI that utilizes the Metro design language. 
    /// It extends the DocumentUIServiceBase and implements interfaces for asynchronous document management and disposal. 
    /// This service allows for the creation, binding, and lifecycle management of tabbed documents within controls such as MetroTabControl, 
    /// UserControl, and Window.
    /// </summary>
    [TargetType(typeof(MetroTabControl))]
    [TargetType(typeof(UserControl))]
    [TargetType(typeof(Window))]
    public sealed class MetroTabbedDocumentUIService : DocumentUIServiceBase, IAsyncDocumentManagerService, IAsyncDisposable
    {
        #region TabbedDocument

        private class TabbedDocument : AsyncDisposable, IAsyncDocument, IDocumentInfo
        {
            private readonly Lifetime _lifetime = new();
            private bool _isClosing;

            public TabbedDocument(MetroTabbedDocumentUIService owner, MetroTabItem tab, string? documentType)
            {
                _ = owner ?? throw new ArgumentNullException(nameof(owner));
                Tab = tab ?? throw new ArgumentNullException(nameof(tab));
                DocumentType = documentType;
                State = DocumentState.Hidden;

                _lifetime.AddBracket(() => owner._documents.Add(this), () => owner._documents.Remove(this));
                _lifetime.Add(() => TabControl?.Items.Remove(Tab));
                _lifetime.Add(() => Tab.ClearStyle());
                _lifetime.Add(DetachContent);
                _lifetime.AddBracket(() => SetDocument(Tab, this), () => SetDocument(Tab, null));
                _lifetime.AddBracket(
                    () => Tab.IsVisibleChanged += OnTabIsVisibleChanged,
                    () => Tab.IsVisibleChanged -= OnTabIsVisibleChanged);
                _lifetime.AddBracket(
                    () => SetTitleBinding(Tab.Content, HeaderedContentControl.HeaderProperty, Tab, true),
                    () => ClearTitleBinding(HeaderedContentControl.HeaderProperty, Tab));

                var dpd = DependencyPropertyDescriptor.FromProperty(HeaderedContentControl.HeaderProperty, typeof(HeaderedContentControl));
                Debug.Assert(dpd != null);
                if (dpd != null)
                {
                    _lifetime.AddBracket(
                        () => dpd.AddValueChanged(Tab, OnTabHeaderChanged),
                        () => dpd.RemoveValueChanged(Tab, OnTabHeaderChanged));
                }
            }

            #region Properties

            public object Id { get; set; } = null!;

            public object Content => ViewHelper.GetViewModelFromView(Tab.Content);

            public object? Title
            {
                get => Tab?.Header;
                set => Tab.Do(x => x.Header = Convert.ToString(value));
            }

            public bool DestroyOnClose { get; set; }

            public DocumentState State { get; private set; }

            public string? DocumentType { get; }

            private MetroTabItem Tab { get; set; }

            private MetroTabControl? TabControl => Tab.With(x => (x.Parent as MetroTabControl)!);

            #endregion

            #region Event Handlers

            private void OnTabHeaderChanged(object? sender, EventArgs e)
            {
                OnPropertyChanged(EventArgsCache.TitlePropertyChanged);
            }

            private void OnTabIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                if (Tab.Content is UIElement element)
                {
                    element.Visibility = Tab.Visibility;
                }
            }

            #endregion

            #region Methods

            public void Close(bool force = true)
            {
                Debug.Assert(false, $"Use {nameof(CloseAsync)} method instead.");
                throw new NotSupportedException($"Use {nameof(CloseAsync)} method instead.");
            }

            public async ValueTask CloseAsync(bool force = true)
            {
                if (_isClosing || IsDisposing)
                {
                    return;
                }
                try
                {
                    _isClosing = true;
                    await CloseCoreAsync(force, DestroyOnClose);
                }
                finally
                {
                    _isClosing = false;
                }
            }

            private async ValueTask CloseCoreAsync(bool force, bool dispose)
            {
                if (State == DocumentState.Destroyed)
                {
                    return;
                }
                if (!force)
                {
                    var cancelEventArgs = new CancelEventArgs();
                    DocumentViewModelHelper.OnClose(Content, cancelEventArgs);
                    if (cancelEventArgs.Cancel)
                    {
                        return;
                    }
                }
                CloseTab();
                State = DocumentState.Hidden;
                if (dispose)
                {
                    await DisposeAsync().ConfigureAwait(false);
                }
            }

            private void CloseTab()
            {
                Tab.Visibility = Visibility.Collapsed;
                if (Tab.CloseTabCommand != null)
                {
                    if (Tab.CloseTabCommand is RoutedCommand command)
                    {
                        command.Execute(Tab.CloseTabCommandParameter, Tab);
                    }
                    else if (Tab.CloseTabCommand.CanExecute(Tab.CloseTabCommandParameter))
                    {
                        Tab.CloseTabCommand.Execute(Tab.CloseTabCommandParameter);
                    }
                }
            }

            private void DetachContent()
            {
                var view = Tab.Content;
                Debug.Assert(view != null);
                //First, detach DataContext from view
                view.With(x => x as FrameworkElement).Do(x => x!.DataContext = null);
                view.With(x => x as FrameworkContentElement).Do(x => x!.DataContext = null);
                view.With(x => x as ContentPresenter).Do(x => x!.Content = null);
                //Second, detach Content from tab item
                Debug.Assert(Tab != null);
                Tab.Do(x => x!.Content = null);
            }

            protected override async ValueTask OnDisposeAsync()
            {
                if (State == DocumentState.Destroyed)
                {
                    return;
                }

                var content = Content;
                try
                {
                    Debug.Assert(content is IAsyncDisposable);
                    if (content is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                }
                finally
                {
                    _lifetime.Dispose();
                }
                DocumentViewModelHelper.OnDestroy(content);
                Tab = null!;
                State = DocumentState.Destroyed;
            }

            public void Hide()
            {
                if (State == DocumentState.Visible)
                {
                    Tab.Visibility = Visibility.Collapsed;
                    State = DocumentState.Hidden;
                }
            }

            public void Show()
            {
                if (State == DocumentState.Hidden)
                {
                    Tab.Visibility = Visibility.Visible;
                }
                Tab.IsSelected = true;
                State = DocumentState.Visible;
            }

            #endregion
        }

        #endregion

        private readonly ObservableCollection<IAsyncDocument> _documents = new();
        private bool _isInitialized;
        private bool _isActiveDocumentChanging;
        private IDisposable? _subscription;

        #region Dependency Properties

        public static readonly DependencyProperty ActiveDocumentProperty = DependencyProperty.Register(
            nameof(ActiveDocument), typeof(IAsyncDocument), typeof(MetroTabbedDocumentUIService), 
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((MetroTabbedDocumentUIService)d).OnActiveDocumentChanged(e.OldValue as IAsyncDocument, e.NewValue as IAsyncDocument)));

        public static readonly DependencyProperty CloseButtonEnabledProperty = DependencyProperty.Register(
            nameof(CloseButtonEnabled), typeof(bool), typeof(MetroTabbedDocumentUIService), 
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target), typeof(MetroTabControl), typeof(MetroTabbedDocumentUIService), 
            new PropertyMetadata(null, (d, e) => ((MetroTabbedDocumentUIService)d).OnTargetChanged((MetroTabControl?)e.OldValue)));

        public static readonly DependencyProperty UnresolvedViewTypeProperty = DependencyProperty.Register(
            nameof(UnresolvedViewType), typeof(Type), typeof(MetroTabbedDocumentUIService));

        #endregion

        public MetroTabbedDocumentUIService()
        {
            if (ViewModelBase.IsInDesignMode) return;
            (_documents as INotifyPropertyChanged).PropertyChanged += OnDocumentsPropertyChanged;
        }

        #region Events

        public event ActiveDocumentChangedEventHandler? ActiveDocumentChanged;

        #endregion

        #region Properties

        public IAsyncDocument? ActiveDocument
        {
            get => (IAsyncDocument)GetValue(ActiveDocumentProperty);
            set => SetValue(ActiveDocumentProperty, value);
        }

        private MetroTabControl? ActualTarget => Target ?? (AssociatedObject as MetroTabControl);

        public IEnumerable<IAsyncDocument> Documents => _documents;

        public bool CloseButtonEnabled
        {
            get => (bool)GetValue(CloseButtonEnabledProperty);
            set => SetValue(CloseButtonEnabledProperty, value);
        }

        public int Count => _documents.Count;

        private MetroTabControl? Target
        {
            get => (MetroTabControl)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        public Type? UnresolvedViewType
        {
            get => (Type)GetValue(UnresolvedViewTypeProperty);
            set => SetValue(UnresolvedViewTypeProperty, value);
        }

        #endregion

        #region Event Handlers

        private void OnActiveDocumentChanged(IAsyncDocument? oldValue, IAsyncDocument? newValue)
        {
            if (!_isActiveDocumentChanging)
            {
                try
                {
                    _isActiveDocumentChanging = true;
                    newValue?.Show();
                }
                finally
                {
                    _isActiveDocumentChanging = false;
                }
            }
            ActiveDocumentChanged?.Invoke(this, new ActiveDocumentChangedEventArgs(oldValue, newValue));
        }

        private void OnDocumentsPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_documents.Count))
            {
                RaisePropertyChanged(nameof(Count));
            }
        }

        private static async void OnTabControlItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems is { Count: > 0 })
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is not MetroTabItem tab)
                            {
                                continue;
                            }
                            if (GetDocument(tab) is IAsyncDocument document)
                            {
                                await document.CloseAsync(force: true);
                            }
                        }
                    }
                    break;
            }
        }

        private void OnTabControlLoaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(Equals(sender, ActualTarget));
            if (sender is FrameworkElement fe)
            {
                fe.Loaded -= OnTabControlLoaded;
            }
            Initialize();
        }

        private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isActiveDocumentChanging)
            {
                return;
            }
            MetroTabControl tabControl = (MetroTabControl)sender;
            if (ActualTarget == tabControl)
            {
                try
                {
                    _isActiveDocumentChanging = true;
                    ActiveDocument = (tabControl.SelectedItem is TabItem tabItem) ? (IAsyncDocument)GetDocument(tabItem) : null;
                }
                finally
                {
                    _isActiveDocumentChanging = false;
                }
            }
        }

        private static void OnTabControlTabItemClosingEvent(object? sender, BaseMetroTabControl.TabItemClosingEventArgs e)
        {
            var viewModel = ViewHelper.GetViewModelFromView(e.ClosingTabItem.Content);
            if (viewModel is IDocumentContent documentContent)
            {
                documentContent.OnClose(e);
            }
        }

        private void OnTargetChanged(MetroTabControl? oldValue)
        {
            Debug.Assert(oldValue == null);
            Initialize();
        }

        #endregion

        #region Methods

        public IAsyncDocument CreateDocument(string? documentType, object? viewModel, object? parameter, object? parentViewModel)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(ActualTarget);
#else
            Throw.IfNull(ActualTarget);
#endif
            object? view;
            if (documentType == null && ViewTemplate == null && ViewTemplateSelector == null)
            {
                view = GetUnresolvedViewType() ?? (ViewLocator ?? UI.ViewLocator.Default).ResolveView(documentType);
                ViewHelper.InitializeView(view, viewModel, parameter, parentViewModel);
            }
            else
            {
                view = CreateAndInitializeView(documentType, viewModel, parameter, parentViewModel);
            }
            var tab = new MetroTabItem
            {
                Header = "Item",
                Content = view,
                CloseButtonEnabled = CloseButtonEnabled
            };
            ActualTarget?.Items.Add(tab);
            var document = new TabbedDocument(this, tab, documentType);
            return document;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_documents.Count == 0)
                {
                    return;
                }
                await Task.WhenAll(_documents.ToList().Select(x => x.CloseAsync().AsTask()));
                if (_documents.Count == 0)
                {
                    return;
                }

                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                void OnDocumentsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
                {
                    if (_documents.Count == 0)
                    {
                        tcs.TrySetResult(true);
                    }
                }

                _documents.CollectionChanged += OnDocumentsCollectionChanged;
                try
                {
                    if (_documents.Count == 0)
                    {
                        tcs.TrySetResult(true);
                    }
                    await tcs.Task.ConfigureAwait(false);
                }
                finally
                {
                    _documents.CollectionChanged -= OnDocumentsCollectionChanged;
                }
            }
            finally
            {
                (_documents as INotifyPropertyChanged).PropertyChanged -= OnDocumentsPropertyChanged;
            }
        }

        private object? GetUnresolvedViewType()
        {
            return UnresolvedViewType == null ? null : Activator.CreateInstance(UnresolvedViewType);
        }

        private void Initialize()
        {
            _isInitialized = true;
            Disposable.DisposeAndNull(ref _subscription);
            _subscription = SubscribeTabControl(ActualTarget);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            Debug.Assert(_subscription == null);
            if (!_isInitialized)
            {
                if (AssociatedObject.IsLoaded)
                {
                    Initialize();
                }
                else
                {
                    AssociatedObject.Loaded += OnTabControlLoaded;
                }
            }
        }

        protected override void OnDetaching()
        {
            _isInitialized = false;
            AssociatedObject.Loaded -= OnTabControlLoaded;
            Debug.Assert(_subscription != null);
            Disposable.DisposeAndNull(ref _subscription);
            base.OnDetaching();
        }

        private IDisposable? SubscribeTabControl(MetroTabControl? tabControl)
        {
            if (tabControl == null)
            {
                return null;
            }
            if (tabControl.ItemsSource != null)
            {
                throw new InvalidOperationException("Can't use not null ItemsSource in this service");
            }
            var lifetime = new Lifetime();
            if (tabControl.Items is INotifyCollectionChanged collection)
            {
                lifetime.AddBracket(() => collection.CollectionChanged += OnTabControlItemsCollectionChanged,
                    () => collection.CollectionChanged -= OnTabControlItemsCollectionChanged);
            }
            lifetime.AddBracket(() => tabControl.SelectionChanged += OnTabControlSelectionChanged,
                () => tabControl.SelectionChanged -= OnTabControlSelectionChanged);
            lifetime.AddBracket(() => tabControl.TabItemClosingEvent += OnTabControlTabItemClosingEvent,
                () => tabControl.TabItemClosingEvent -= OnTabControlTabItemClosingEvent);
            return lifetime;
        }

        #endregion
    }

    internal static partial class EventArgsCache
    {
        internal static readonly PropertyChangedEventArgs TitlePropertyChanged = new(nameof(IAsyncDocument.Title));
    }
}