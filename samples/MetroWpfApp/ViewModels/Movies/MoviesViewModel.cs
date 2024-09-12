using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using MetroWpfApp.Interfaces.Services;
using MetroWpfApp.Interfaces.ViewModels;
using MetroWpfApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Data;

namespace MetroWpfApp.ViewModels
{
    internal sealed partial class MoviesViewModel: DocumentContentViewModelBase
    {
        #region Properties

        public ObservableCollection<MovieModelBase> Movies
        {
            get => GetProperty(() => Movies);
            private set { SetProperty(() => Movies, value); }
        }

        public ListCollectionView MoviesView
        {
            get => GetProperty(() => MoviesView);
            private set { SetProperty(() => MoviesView, value); }
        }

        public MovieModelBase? SelectedItem
        {
            get => GetProperty(() => SelectedItem);
            set { SetProperty(() => SelectedItem, value, OnSelectedItemChanged); }
        }

        #endregion

        #region Services

        private IDialogCoordinator DialogCoordinator => GetService<IDialogCoordinator>();

        private IAsyncDialogService DialogService => GetService<IAsyncDialogService>();

        private IMoviesService MoviesService => GetService<IMoviesService>();

        private IMainWindowViewModel? ParentViewModel => (this as ISupportParentViewModel).ParentViewModel as IMainWindowViewModel;

        private ISettingsService SettingsService => GetService<ISettingsService>();


        #endregion

        #region Event Handlers

        protected override void OnInitializeInRuntime()
        {
            base.OnInitializeInRuntime();

            CreateCommands();
            CreateSettings();

            Movies = new ObservableCollection<MovieModelBase>();
            Lifetime.Add(Movies.Clear);

            MoviesView = new ListCollectionView(Movies);
            Lifetime.Add(MoviesView.DetachFromSourceCollection);
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (LoadSettings())
            {
                if (!string.IsNullOrEmpty(Settings.SelectedPath))
                {
                    SelectedItem = Movies.FindByPath(Settings.SelectedPath);
                }
            }
        }

        private void OnSelectedItemChanged(MovieModelBase? oldSelectedItem)
        {
            var newSelectedItem = SelectedItem;
            if (newSelectedItem != null)
            {
            }
        }

        #endregion

        #region Methods

        protected override async ValueTask OnDisposeAsync()
        {
            Settings.SelectedPath = SelectedItem?.GetPath();

            await base.OnDisposeAsync();
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(DialogCoordinator != null, $"{nameof(DialogCoordinator)} is null");
            Debug.Assert(DialogService != null, $"{nameof(DialogService)} is null");
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            Debug.Assert(ParentViewModel != null, $"{nameof(ParentViewModel)} is null");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

            await ReloadMoviesAsync(cancellationToken);
        }

        private async ValueTask ReloadMoviesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Movies.Clear();
            var movies = await MoviesService.GetMoviesAsync(cancellationToken);
            movies.ForEach(Movies.Add);
            Movies.OfType<MovieGroupModel>().FirstOrDefault()?.Expand();
        }

        #endregion
    }
}
