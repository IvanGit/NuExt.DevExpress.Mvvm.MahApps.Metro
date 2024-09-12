using ControlzEx.Theming;
using DevExpress.Mvvm;
using MetroWpfApp.Services;
using MetroWpfApp.Views;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MetroWpfApp.ViewModels
{
    partial class MainWindowViewModel
    {
        #region Commands

        public ICommand? ActiveDocumentChangedCommand
        {
            get => GetProperty(() => ActiveDocumentChangedCommand);
            private set { SetProperty(() => ActiveDocumentChangedCommand, value); }
        }

        public ICommand? ChangeAccentColorCommand
        {
            get => GetProperty(() => ChangeAccentColorCommand);
            private set { SetProperty(() => ChangeAccentColorCommand, value); }
        }

        public ICommand? ChangeAppThemeCommand
        {
            get => GetProperty(() => ChangeAppThemeCommand);
            private set { SetProperty(() => ChangeAppThemeCommand, value); }
        }

        public ICommand? ShowMoviesCommand
        {
            get => GetProperty(() => ShowMoviesCommand);
            private set { SetProperty(() => ShowMoviesCommand, value); }
        }

        #endregion


        #region Command Methods

        private bool CanShowMovies()
        {
            return IsUsable;
        }

        private async Task ShowMoviesAsync()
        {
            var command = GetAsyncCommand();
            Debug.Assert(command == ShowMoviesCommand);
            var cancellationToken = command!.CancellationTokenSource.Token;

            var document = await DocumentManagerService.FindDocumentByIdOrCreateAsync(default(Movies),
                async x =>
                {
                    var vm = new MoviesViewModel();
                    var doc = x.CreateDocument(nameof(MoviesView), vm, null, this);
                    doc.DestroyOnClose = true;
                    doc.Title = "Movies";
                    try
                    {
                        await vm.InitializeAsync(cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(ex is OperationCanceledException, ex.Message);
                        if (doc is IAsyncDisposable asyncDisposable)
                        {
                            await asyncDisposable.DisposeAsync();
                        }
                        throw;
                    }
                    return doc;
                });
            document.Show();
        }

        private bool CanChangeAccentColor(string? colorScheme)
        {
            return IsUsable && !string.IsNullOrEmpty(colorScheme);
        }

        private static void ChangeAccentColor(string? colorScheme)
        {
            if (colorScheme is not null)
            {
                ThemeManager.Current.ChangeThemeColorScheme(Application.Current, colorScheme);
            }
        }

        private bool CanChangeAppTheme(string? baseColorScheme)
        {
            return IsUsable && !string.IsNullOrEmpty(baseColorScheme);
        }

        private static void ChangeAppTheme(string? baseColorScheme)
        {
            if (baseColorScheme is not null)
            {
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, baseColorScheme);
            }
        }

        protected override async ValueTask OnContentRenderedAsync(CancellationToken cancellationToken)
        {
            await base.OnContentRenderedAsync(cancellationToken);
            Debug.Assert(ApplicationService != null, $"{nameof(ApplicationService)} is null");
            Debug.Assert(DocumentManagerService is IAsyncDisposable, $"{nameof(DocumentManagerService)} is not {nameof(IAsyncDisposable)}");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

            Debug.Assert(CheckAccess());
            cancellationToken.ThrowIfCancellationRequested();

            if (DocumentManagerService is IAsyncDisposable asyncDisposable)
            {
                Lifetime.AddAsyncDisposable(asyncDisposable);
            }

            await MoviesService.InitializeAsync(cancellationToken);

            Debug.Assert(Settings.IsSuspended == false);
            if (Settings.MoviesOpened)
            {
                ShowMoviesCommand?.Execute(null);
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            LoadSettings();
        }

        #endregion

        #region Methods

        private void CreateCommands()
        {
            ActiveDocumentChangedCommand = RegisterCommand(UpdateTitle);
            ChangeAccentColorCommand = RegisterCommand<string?>(ChangeAccentColor, CanChangeAccentColor);
            ChangeAppThemeCommand = RegisterCommand<string?>(ChangeAppTheme, CanChangeAppTheme);
            ShowMoviesCommand = RegisterAsyncCommand(ShowMoviesAsync, CanShowMovies);
        }

        #endregion
    }
}
