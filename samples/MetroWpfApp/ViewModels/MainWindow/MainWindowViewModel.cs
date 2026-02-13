using ControlzEx.Theming;
using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using MovieWpfApp.Interfaces.Services;
using MovieWpfApp.Interfaces.ViewModels;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Media;

namespace MovieWpfApp.ViewModels
{
    internal sealed partial class MainWindowViewModel : WindowViewModel, IMainWindowViewModel
    {
        #region Properties

        public IAsyncDocument? ActiveDocument
        {
            get => GetProperty(() => ActiveDocument);
            set { SetProperty(() => ActiveDocument, value, OnActiveDocumentChanged); }
        }

        public ObservableCollection<IMenuItemViewModel> MenuItems { get; } = [];

        #endregion

        #region Services

        public IApplicationService ApplicationService => GetService<IApplicationService>()!;

        private IDialogCoordinator? DialogCoordinator => GetService<IDialogCoordinator>();

        public IAsyncDocumentManagerService? DocumentManagerService => GetService<IAsyncDocumentManagerService>("Documents");

        public IEnvironmentService EnvironmentService => GetService<IEnvironmentService>()!;

        public ILogger Logger => GetService<ILogger>()!;

        private IMessageBoxService? MessageBoxService => GetService<IMessageBoxService>();

        private IMoviesService MoviesService => GetService<IMoviesService>()!;

        private ISettingsService? SettingsService => GetService<ISettingsService>();

        #endregion

        #region Event Handlers

        private void OnActiveDocumentChanged(IAsyncDocument? oldActiveDocument)
        {
        }

        #endregion

        #region Methods

        private ValueTask LoadMenuAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            MenuItems.Clear();
            var menuItems = new IMenuItemViewModel[]
            {
                new MenuItemViewModel()
                {
                    Header = Loc.File,
                    SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(new IMenuItemViewModel?[]
                    {
                        new MenuItemViewModel() { Header = Loc.Movies, Command = ShowMoviesCommand },
                        null,
                        new MenuItemViewModel() { Header = Loc.Exit, Command = CloseCommand }
                    })
                },
                new MenuItemViewModel()
                {
                    Header = Loc.View,
                    SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(new IMenuItemViewModel?[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = Loc.Theme,
                            SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(ThemeManager.Current.Themes
                                .GroupBy(x => x.BaseColorScheme)
                                .Select(x => x.First())
                                .Select(a => new AppThemeMenuItemViewModel { Header = a.BaseColorScheme, BorderColorBrush = (a.Resources["MahApps.Brushes.ThemeForeground"] as Brush)!, ColorBrush = (a.Resources["MahApps.Brushes.ThemeBackground"] as Brush)!, Command = ChangeAppThemeCommand, CommandParameter = a.BaseColorScheme}))
                        },
                        new MenuItemViewModel()
                        {
                            Header = Loc.Accent,
                            SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(ThemeManager.Current.Themes
                                .GroupBy(x => x.ColorScheme)
                                .OrderBy(a => a.Key)
                                .Select(a => new AccentColorMenuItemViewModel { Header = a.Key, ColorBrush = a.First().ShowcaseBrush, Command = ChangeAccentColorCommand, CommandParameter = a.Key }))
                        },
                        null,
                        new MenuItemViewModel() { Header = Loc.Hide_Active_Document, CommandParameter = false, Command = ShowHideActiveDocumentCommand },
                        new MenuItemViewModel() { Header = Loc.Show_Active_Document, CommandParameter = true, Command = ShowHideActiveDocumentCommand },
                        new MenuItemViewModel() { Header = Loc.Close_Active_Document, Command = CloseActiveDocumentCommand }
                    })
                }
            };
            menuItems.ForEach(MenuItems.Add);
            return default;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            var doc = DocumentManagerService?.FindDocumentById(default(Movies));
            Settings!.MoviesOpened = doc is not null;

            await base.OnDisposeAsync();
        }

        protected override async void OnError(Exception ex, [CallerMemberName] string? callerName = null)
        {
            base.OnError(ex, callerName);
#pragma warning disable IDE0079
#pragma warning disable CA2254
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(ex, Loc.An_error_has_occurred_in__Caller____Exception_, callerName, ex.Message);
            }
#pragma warning restore CA2254
#pragma warning restore IDE0079
            if (DialogCoordinator == null)
            {
                MessageBoxService?.ShowMessage(string.Format(Loc.An_error_has_occurred_in_Arg0_Arg1, callerName, ex.Message), Loc.Error, MessageButton.OK, MessageIcon.Error);
            }
            else
            {
                var dialogSettings = new MetroDialogSettings
                {
                    AffirmativeButtonText = Loc.OK,
                    CancellationToken = CancellationTokenSource.Token,
                };
                await DialogCoordinator.ShowMessageAsync(this, Loc.Error, string.Format(Loc.An_error_has_occurred_in_Arg0_Arg1, callerName, ex.Message), MessageDialogStyle.Affirmative, dialogSettings).ConfigureAwait(false);
            }
        }

        protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(ApplicationService != null, $"{nameof(ApplicationService)} is null");
            Debug.Assert(DialogCoordinator != null, $"{nameof(DialogCoordinator)} is null");
            Debug.Assert(DocumentManagerService != null, $"{nameof(DocumentManagerService)} is null");
            Debug.Assert(EnvironmentService != null, $"{nameof(EnvironmentService)} is null");
            Debug.Assert(Logger != null, $"{nameof(Logger)} is null");
            Debug.Assert(MessageBoxService != null, $"{nameof(MessageBoxService)} is null");
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

            Lifetime.AddAsyncDisposable(DocumentManagerService!);

            cancellationToken.ThrowIfCancellationRequested();
            return default;
        }

        private void UpdateTitle()
        {
            var sb = new ValueStringBuilder(stackalloc char[128]);
            var doc = ActiveDocument;
            if (doc != null)
            {
                sb.Append($"{doc.Title} - ");
            }
            sb.Append($"{AssemblyInfo.Current.Product} v{AssemblyInfo.Current.Version?.ToString(3)}");
            Title = sb.ToString();
        }

        #endregion
    }
}
