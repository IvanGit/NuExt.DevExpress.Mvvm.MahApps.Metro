using ControlzEx.Theming;
using DevExpress.Mvvm;
using MetroWpfApp.Models;
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

        public ICommand? CloseActiveDocumentCommand
        {
            get => GetProperty(() => CloseActiveDocumentCommand);
            private set { SetProperty(() => CloseActiveDocumentCommand, value); }
        }

        public ICommand? ShowHideActiveDocumentCommand
        {
            get => GetProperty(() => ShowHideActiveDocumentCommand);
            private set { SetProperty(() => ShowHideActiveDocumentCommand, value); }
        }

        public ICommand? ShowMoviesCommand
        {
            get => GetProperty(() => ShowMoviesCommand);
            private set { SetProperty(() => ShowMoviesCommand, value); }
        }

        public IAsyncCommand? OpenMovieCommand
        {
            get => GetProperty(() => OpenMovieCommand);
            private set { SetProperty(() => OpenMovieCommand, value); }
        }

        public IAsyncCommand? CloseMovieCommand
        {
            get => GetProperty(() => CloseMovieCommand);
            private set { SetProperty(() => CloseMovieCommand, value); }
        }

        #endregion

        #region Command Methods

        private bool CanCloseActiveDocument()
        {
            return IsUsable && ActiveDocument != null;
        }

        private async Task CloseActiveDocumentAsync()
        {
            await ActiveDocument!.CloseAsync();
        }

        private bool CanShowHideActiveDocument(bool show)
        {
            return IsUsable && ActiveDocument != null;
        }

        private void ShowHideActiveDocument(bool show)
        {
            if (show)
            {
                ActiveDocument?.Show();
            }
            else
            {
                ActiveDocument?.Hide();
            }
        }

        private bool CanShowMovies()
        {
            return IsUsable && DocumentManagerService != null;
        }

        private async Task ShowMoviesAsync()
        {
            var cancellationToken = GetCurrentCancellationToken();

            var document = await DocumentManagerService!.FindDocumentByIdOrCreateAsync(default(Movies),
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
            return IsUsable;
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
            return IsUsable;
        }

        private static void ChangeAppTheme(string? baseColorScheme)
        {
            if (baseColorScheme is not null)
            {
                ThemeManager.Current.ChangeThemeBaseColor(Application.Current, baseColorScheme);
            }
        }

        private bool CanOpenMovie(MovieModel movie)
        {
            return IsUsable && DocumentManagerService != null;
        }

        private async Task OpenMovieAsync(MovieModel movie)
        {
            var cancellationToken = GetCurrentCancellationToken();

            var document = await DocumentManagerService!.FindDocumentByIdOrCreateAsync(new MovieDocument(movie), async x =>
            {
                var vm = new MovieViewModel();
                var doc = x.CreateDocument(nameof(MovieView), vm, movie, this);
                doc.DestroyOnClose = true;
                try
                {
                    await vm.InitializeAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    Debug.Assert(ex is OperationCanceledException, ex.Message);
                    //await vm.DisposeAsync();
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

        private bool CanCloseMovie(MovieModel movie) => CanOpenMovie(movie);

        private async Task CloseMovieAsync(MovieModel movie)
        {
            var doc = DocumentManagerService!.FindDocumentById(new MovieDocument(movie));
            if (doc == null) return;
            await doc.CloseAsync();
        }

        #endregion

        #region Methods

        protected override void CreateCommands()
        {
            base.CreateCommands();
            ActiveDocumentChangedCommand = RegisterCommand(UpdateTitle);
            ChangeAccentColorCommand = RegisterCommand<string?>(ChangeAccentColor, CanChangeAccentColor);
            ChangeAppThemeCommand = RegisterCommand<string?>(ChangeAppTheme, CanChangeAppTheme);
            ShowMoviesCommand = RegisterAsyncCommand(ShowMoviesAsync, CanShowMovies);
            ShowHideActiveDocumentCommand = RegisterCommand<bool>(ShowHideActiveDocument, CanShowHideActiveDocument);
            CloseActiveDocumentCommand = RegisterAsyncCommand(CloseActiveDocumentAsync, CanCloseActiveDocument);
            OpenMovieCommand = RegisterAsyncCommand<MovieModel>(OpenMovieAsync, CanOpenMovie);
            CloseMovieCommand = RegisterAsyncCommand<MovieModel>(CloseMovieAsync, CanCloseMovie);
        }

        protected override async ValueTask OnContentRenderedAsync(CancellationToken cancellationToken)
        {
            await base.OnContentRenderedAsync(cancellationToken);

            Debug.Assert(CheckAccess());
            cancellationToken.ThrowIfCancellationRequested();

            await LoadMenuAsync(cancellationToken);

            await MoviesService.InitializeAsync(cancellationToken);

            Debug.Assert(Settings!.IsSuspended == false);
            if (Settings.MoviesOpened)
            {
                ShowMoviesCommand?.Execute(null);
            }
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            CreateSettings();
            UpdateTitle();
        }

        #endregion
    }
}
