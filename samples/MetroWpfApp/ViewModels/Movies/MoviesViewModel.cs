using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using MetroWpfApp.Interfaces.Services;
using MetroWpfApp.Interfaces.ViewModels;
using MetroWpfApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace MetroWpfApp.ViewModels
{
    internal sealed partial class MoviesViewModel : DocumentContentViewModelBase
    {
        #region Properties

        public ObservableCollection<MovieModelBase>? Movies
        {
            get => GetProperty(() => Movies);
            private set { SetProperty(() => Movies, value); }
        }

        public ListCollectionView? MoviesView
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

        private IDialogCoordinator? DialogCoordinator => GetService<IDialogCoordinator>();

        private IAsyncDialogService? DialogService => GetService<IAsyncDialogService>();

        public IEnvironmentService EnvironmentService => GetService<IEnvironmentService>()!;

        private IMessageBoxService? MessageBoxService => GetService<IMessageBoxService>();

        private IMoviesService MoviesService => GetService<IMoviesService>();

        private IMainWindowViewModel? ParentViewModel => (this as ISupportParentViewModel).ParentViewModel as IMainWindowViewModel;

        private ISettingsService? SettingsService => GetService<ISettingsService>();

        #endregion

        #region Event Handlers

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
            Settings!.SelectedPath = SelectedItem?.GetPath();

            await base.OnDisposeAsync();
        }

        protected override void OnError(Exception ex, [CallerMemberName] string? callerName = null)
        {
            base.OnError(ex, callerName);
            MessageBoxService.Show($"An error has occurred in {callerName}:{Environment.NewLine}{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(DialogCoordinator != null, $"{nameof(DialogCoordinator)} is null");
            Debug.Assert(DialogService != null, $"{nameof(DialogService)} is null");
            Debug.Assert(EnvironmentService != null, $"{nameof(EnvironmentService)} is null");
            Debug.Assert(MessageBoxService != null, $"{nameof(MessageBoxService)} is null");
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            Debug.Assert(ParentViewModel != null, $"{nameof(ParentViewModel)} is null");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

            await ReloadMoviesAsync(cancellationToken);
        }

        protected override void OnInitializeInRuntime()
        {
            base.OnInitializeInRuntime();

            Movies = new ObservableCollection<MovieModelBase>();
            Lifetime.Add(Movies.Clear);

            MoviesView = new ListCollectionView(Movies);
            Lifetime.Add(MoviesView.DetachFromSourceCollection);
        }

        private async ValueTask ReloadMoviesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Movies!.Clear();
            var movies = await MoviesService.GetMoviesAsync(cancellationToken);
            movies.ForEach(Movies.Add);
            Movies.OfType<MovieGroupModel>().FirstOrDefault()?.Expand();
        }

        #endregion
    }
}
