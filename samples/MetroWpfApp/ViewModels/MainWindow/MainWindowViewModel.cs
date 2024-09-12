using ControlzEx.Theming;
using DevExpress.Mvvm;
using MetroWpfApp.Interfaces.Services;
using MetroWpfApp.Interfaces.ViewModels;
using MetroWpfApp.Models;
using MetroWpfApp.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Media;

namespace MetroWpfApp.ViewModels
{
    internal sealed partial class MainWindowViewModel : WindowViewModel, IMainWindowViewModel
    {

        #region Properties

        public IAsyncDocument? ActiveDocument
        {
            get => GetProperty(() => ActiveDocument);
            set { SetProperty(() => ActiveDocument, value, OnActiveDocumentChanged); }
        }

        public ObservableCollection<IMenuItemViewModel> MenuItems { get; } = new();

        #endregion

        #region Services

        public IApplicationService ApplicationService => GetService<IApplicationService>();

        public IAsyncDocumentManagerService DocumentManagerService => GetService<IAsyncDocumentManagerService>("Documents");

        private IMoviesService MoviesService => GetService<IMoviesService>();

        private ISettingsService SettingsService => GetService<ISettingsService>();

        #endregion

        #region Event Handlers

        private void OnActiveDocumentChanged(IAsyncDocument? oldActiveDocument)
        {
        }

        #endregion

        #region Methods

        public async ValueTask CloseMovieAsync(MovieModel movie, CancellationToken cancellationToken)
        {
            var doc = DocumentManagerService.FindDocumentById(new MovieDocument(movie));
            if (doc == null) return;
            await doc.CloseAsync();
        }

        private ValueTask LoadMenuAsync(CancellationToken cancellationToken)
        {
            MenuItems.Clear();
            cancellationToken.ThrowIfCancellationRequested();
            var menuItems = new IMenuItemViewModel[]
            {
                new MenuItemViewModel()
                {
                    Header = "File",
                    SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(new IMenuItemViewModel?[]
                    {
                        new MenuItemViewModel() { Header = "Movies", Command = ShowMoviesCommand },
                        null,
                        new MenuItemViewModel() { Header = "Exit", Command = CloseCommand }
                    })
                },
                new MenuItemViewModel()
                {
                    Header = "View",
                    SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(new IMenuItemViewModel?[]
                    {
                        new MenuItemViewModel()
                        {
                            Header = "Theme",
                            SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(ThemeManager.Current.Themes
                                .GroupBy(x => x.BaseColorScheme)
                                .Select(x => x.First())
                                .Select(a => new AppThemeMenuItemViewModel { Header = a.BaseColorScheme, BorderColorBrush = (a.Resources["MahApps.Brushes.ThemeForeground"] as Brush)!, ColorBrush = (a.Resources["MahApps.Brushes.ThemeBackground"] as Brush)!, Command = ChangeAppThemeCommand, CommandParameter = a.BaseColorScheme}))
                        },
                        new MenuItemViewModel()
                        {
                            Header = "Accent",
                            SubMenuItems=new ObservableCollection<IMenuItemViewModel?>(ThemeManager.Current.Themes
                                .GroupBy(x => x.ColorScheme)
                                .OrderBy(a => a.Key)
                                .Select(a => new AccentColorMenuItemViewModel { Header = a.Key, ColorBrush = a.First().ShowcaseBrush, Command = ChangeAccentColorCommand, CommandParameter = a.Key }))
                        },
                    })
                }
            };
            menuItems.ForEach(MenuItems.Add);
            return default;
        }

        protected override async ValueTask OnDisposeAsync()
        {
            var doc = DocumentManagerService?.FindDocumentById(default(Movies));
            Settings.MoviesOpened = doc is not null;

            await base.OnDisposeAsync();
        }

        protected override void OnInitializeInRuntime()
        {
            Debug.Assert(DocumentManagerService is null, $"{nameof(DocumentManagerService)} is not null");
            base.OnInitializeInRuntime();
            UpdateTitle();

            CreateCommands();
            CreateSettings();
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            await LoadMenuAsync(cancellationToken);
        }

        public async ValueTask OpenMovieAsync(MovieModel movie, CancellationToken cancellationToken)
        {
            var document = await DocumentManagerService.FindDocumentByIdOrCreateAsync(new MovieDocument(movie), async x =>
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

        private void UpdateTitle()
        {
            var sb = new ValueStringBuilder();
            var doc = ActiveDocument;
            if (doc != null)
            {
                sb.Append($"{doc.Title} - ");
            }
            sb.Append($"{AssemblyInfo.Product} v{AssemblyInfo.Version?.ToString(3)}");
            Title = sb.ToString();
        }

        #endregion

    }
}
