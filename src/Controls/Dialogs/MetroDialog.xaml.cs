using DevExpress.Mvvm;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MahApps.Metro.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for MetroDialog.xaml
    /// </summary>
    public partial class MetroDialog : BaseMetroDialog
    {
        #region Dependency Properties

        /// <summary>Identifies the <see cref="AffirmativeButtonText"/> dependency property.</summary>
        public static readonly DependencyProperty AffirmativeButtonTextProperty = DependencyProperty.Register(
            nameof(AffirmativeButtonText),
            typeof(string),
            typeof(MetroDialog),
            new PropertyMetadata("OK"));

        public string AffirmativeButtonText
        {
            get => (string)GetValue(AffirmativeButtonTextProperty);
            set => SetValue(AffirmativeButtonTextProperty, value);
        }

        /// <summary>Identifies the <see cref="NegativeButtonText"/> dependency property.</summary>
        public static readonly DependencyProperty NegativeButtonTextProperty = DependencyProperty.Register(
            nameof(NegativeButtonText),
            typeof(string),
            typeof(MetroDialog),
            new PropertyMetadata("Cancel"));

        public string NegativeButtonText
        {
            get => (string)GetValue(NegativeButtonTextProperty);
            set => SetValue(NegativeButtonTextProperty, value);
        }

        /// <summary>Identifies the <see cref="CommandsSource"/> dependency property.</summary>
        public static readonly DependencyProperty CommandsSourceProperty = DependencyProperty.Register(
            nameof(CommandsSource),
            typeof(IEnumerable),
            typeof(MetroDialog),
            new PropertyMetadata(null));

        public IEnumerable? CommandsSource
        {
            get => (IEnumerable)GetValue(CommandsSourceProperty);
            set => SetValue(CommandsSourceProperty, value);
        }

        #endregion

        private readonly TaskCompletionSource<UICommand?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IDisposable? _cancellationTokenRegistration;

        public MetroDialog()
            : this(null, null)
        {
        }

        public MetroDialog(MetroWindow? parentWindow)
            : this(parentWindow, null)
        {
        }

        public MetroDialog(MetroDialogSettings? settings)
            : this(null, settings)
        {
        }

        public MetroDialog(MetroWindow? parentWindow, MetroDialogSettings? settings)
            : base(parentWindow, settings)
        {
            InitializeComponent();
            SetCurrentValue(AffirmativeButtonTextProperty, DialogSettings.AffirmativeButtonText);
            SetCurrentValue(NegativeButtonTextProperty, DialogSettings.NegativeButtonText);
        }

        #region Properties

        private UICommand? CancelCommand => CommandsSource?.Cast<UICommand>().FirstOrDefault(c => c.IsCancel);
        private UICommand? DefaultCommand => CommandsSource?.Cast<UICommand>().FirstOrDefault(c => c.IsDefault);

        #endregion

        #region Event Handlers

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Button { DataContext: UICommand command })
            {
                CleanUpHandlers();
                _tcs.TrySetResult(command);
                e.Handled = true;
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e is { Key: Key.System, SystemKey: Key.F4 })
            {
                CleanUpHandlers();

                _tcs.TrySetResult(CancelCommand);

                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                CleanUpHandlers();

                var result = DefaultCommand;
                if (e.OriginalSource is Button { DataContext: UICommand command })
                {
                    result = command;
                }

                _tcs.TrySetResult(result);

                e.Handled = true;
            }
        }

        #endregion

        #region Methods

        private void CleanUpHandlers()
        {
            if (DialogBottom != null && DialogButtons != null)
            {
                foreach (Button button in DependencyObjectExtensions.FindChildren<Button>(DialogButtons))
                {
                    button.Click -= OnButtonClick;
                    button.KeyDown -= OnKeyDownHandler;
                }
            }

            KeyDown -= OnKeyDownHandler;

            Disposable.DisposeAndNull(ref _cancellationTokenRegistration);
        }

        private void SetUpHandlers()
        {
            if (DialogBottom != null && DialogButtons != null)
            {
                foreach (Button button in DependencyObjectExtensions.FindChildren<Button>(DialogButtons))
                {
                    if (button.Command is null && button.DataContext is UICommand command)
                    {
                        button.Click += OnButtonClick;
                        button.KeyDown += OnKeyDownHandler;
                    }
                }
            }

            KeyDown += OnKeyDownHandler;

            _cancellationTokenRegistration = DialogSettings.CancellationToken.Register(() =>
            {
                this.BeginInvoke(() =>
                {
                    CleanUpHandlers();
                    _tcs.TrySetResult(null!);
                });
            });
        }

        public async ValueTask<UICommand?> WaitForButtonPressAsync()
        {
            SetUpHandlers();

            return await _tcs.Task.ConfigureAwait(false);
        }

        #endregion
    }
}
