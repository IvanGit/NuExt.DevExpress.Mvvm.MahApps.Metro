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

        private class TabbedDocument : ViewModelBase, IAsyncDocument, IDocumentInfo, IAsyncDisposable
        {
            private readonly Lifetime _lifetime = new();
            private bool _isClosing;
            private bool _isDisposing;

            public TabbedDocument(MetroTabbedDocumentUIService owner, MetroTabItem tab, object documentContentView, string? documentType)
            {
                _ = owner ?? throw new ArgumentNullException(nameof(owner));
                Tab = tab ?? throw new ArgumentNullException(nameof(tab));
                DocumentContentView = documentContentView;
                DocumentType = documentType;
                State = DocumentState.Hidden;

                _lifetime.AddBracket(() => owner._documents.Add(this), () => owner._documents.Remove(this));
                _lifetime.Add(DetachContent);
                _lifetime.AddBracket(() => SetDocument(tab, this), () => SetDocument(tab, null));
                _lifetime.AddBracket(
                    () => SetTitleBinding(documentContentView, HeaderedContentControl.HeaderProperty, tab, true),
                    () => ClearTitleBinding(HeaderedContentControl.HeaderProperty, tab));

                var dpd = DependencyPropertyDescriptor.FromProperty(HeaderedContentControl.HeaderProperty, typeof(HeaderedContentControl));
                Debug.Assert(dpd != null);
                if (dpd is not null)
                {
                    _lifetime.AddBracket(
                        () => dpd.AddValueChanged(tab, OnTabHeaderChanged),
                        () => dpd.RemoveValueChanged(tab, OnTabHeaderChanged));
                }
            }

            #region Properties

            public object? Id { get; set; }

            public object Content => ViewHelper.GetViewModelFromView(DocumentContentView);

            private object DocumentContentView { get; set; }

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
                RaisePropertyChanged(nameof(Title));
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
                if (_isClosing || _isDisposing)
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
                    await DisposeAsync();
                }
            }

            private void CloseTab()
            {
                Tab.Visibility = Visibility.Hidden;
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
                //First, detach DataContext from view
                Debug.Assert(DocumentContentView != null);
                DocumentContentView.With(x => x as FrameworkElement).Do(x => x!.DataContext = null);
                DocumentContentView.With(x => x as FrameworkContentElement).Do(x => x!.DataContext = null);
                DocumentContentView.With(x => x as ContentPresenter).Do(x => x!.Content = null);
                //Second, detach Content from tab item
                Debug.Assert(Tab != null);
                Tab.Do(x => x!.Content = null);
            }

            public async ValueTask DisposeAsync()
            {
                if (_isDisposing)
                {
                    return;
                }
                if (State == DocumentState.Destroyed)
                {
                    return;
                }
                _isDisposing = true;
                try
                {
                    Tab.ClearStyle();
                    TabControl.Do(x => x!.Items.Remove(Tab));
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
                    DocumentContentView = null!;
                    State = DocumentState.Destroyed;
                }
                finally
                {
                    _isDisposing = false;
                }
            }

            public void Hide()
            {
                if (State == DocumentState.Visible)
                {
                    CloseTab();
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

        #region Dependency Properties

        public static readonly DependencyProperty ActiveDocumentProperty =
           DependencyProperty.Register(nameof(ActiveDocument), typeof(IAsyncDocument), typeof(MetroTabbedDocumentUIService), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) => ((MetroTabbedDocumentUIService)d).OnActiveDocumentChanged(e.OldValue as IAsyncDocument, e.NewValue as IAsyncDocument)));

        public static readonly DependencyProperty CloseButtonEnabledProperty =
            DependencyProperty.Register(nameof(CloseButtonEnabled), typeof(bool), typeof(MetroTabbedDocumentUIService), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register(nameof(Target), typeof(MetroTabControl), typeof(MetroTabbedDocumentUIService), new PropertyMetadata(null, (d, e) => ((MetroTabbedDocumentUIService)d).OnTargetChanged((MetroTabControl)e.OldValue)));

        public static readonly DependencyProperty UnresolvedViewTypeProperty =
            DependencyProperty.Register(nameof(UnresolvedViewType), typeof(Type), typeof(MetroTabbedDocumentUIService), new PropertyMetadata(null));

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

        /*IDocument? IDocumentManagerService.ActiveDocument
        {
            get => ActiveDocument;
            set => ActiveDocument = (IAsyncDocument?)value;
        }*/

        private MetroTabControl? ActualTarget => Target ?? (AssociatedObject as MetroTabControl);

        public IEnumerable<IAsyncDocument> Documents => _documents;

        /*IEnumerable<IDocument> IDocumentManagerService.Documents => _documents;*/

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

        private async void ActualTarget_Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null && e.OldItems.Count > 0)
                    {
                        foreach (var item in e.OldItems)
                        {
                            if (item is MetroTabItem tab)
                            {
                                var document = GetDocument(tab) as IAsyncDocument;
                                Debug.Assert(document != null);
                                if (document is not null)
                                {
                                    await document.CloseAsync(force: true);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        private void ActualTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                    ActiveDocument = (tabControl.SelectedItem is TabItem tabItem) ? (IAsyncDocument)GetDocument(tabItem) : null!;
                }
                finally
                {
                    _isActiveDocumentChanging = false;
                }
            }
        }

        private void ActualTarget_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(false);
        }

        private void OnAssociatedObjectLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            Initialize();
        }

        private void OnTargetChanged(MetroTabControl oldValue)
        {
            Debug.Assert(oldValue == null);//TODO Unsubscribe
            Initialize();
        }

        #endregion

        #region Methods

        /*IDocument IDocumentManagerService.CreateDocument(string? documentType, object? viewModel, object? parameter,
            object? parentViewModel)
        {
            return CreateDocument(documentType, viewModel, parameter, parentViewModel);
        }*/

        public IAsyncDocument CreateDocument(string? documentType, object? viewModel, object? parameter, object? parentViewModel)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(ActualTarget);
#else
            Throw.IfNull(ActualTarget);
#endif
            object view;
            if (documentType == null)
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
            var document = new TabbedDocument(this, tab, view, documentType);
            return document;
        }

        private object? GetUnresolvedViewType()
        {
            if (UnresolvedViewType == null) { return null; }
            return Activator.CreateInstance(UnresolvedViewType);
        }

        private void Initialize()
        {
            _isInitialized = true;
            SubscribeTabControl(ActualTarget);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (!_isInitialized)
            {
                if (AssociatedObject.IsLoaded)
                {
                    Initialize();
                }
                else
                {
                    AssociatedObject.Loaded += OnAssociatedObjectLoaded;
                }
            }
        }

        protected override void OnDetaching()
        {
            _isInitialized = false;
            AssociatedObject.Loaded -= OnAssociatedObjectLoaded;
            base.OnDetaching();
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

        private void SubscribeTabControl(MetroTabControl? actualTarget)
        {
            if (actualTarget == null) return;
            if (actualTarget.ItemsSource != null) throw new InvalidOperationException("Can't use not null ItemsSource in this service");
            actualTarget.Unloaded += ActualTarget_Unloaded;
            //actualTarget.TabItemClosingEvent += ActualTarget_TabItemClosingEvent;
            (actualTarget.Items as INotifyCollectionChanged).Do(x => x.CollectionChanged += ActualTarget_Items_CollectionChanged);
            actualTarget.SelectionChanged += ActualTarget_SelectionChanged;
        }

        #endregion
    }

}