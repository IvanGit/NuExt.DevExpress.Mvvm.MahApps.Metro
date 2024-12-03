using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using MovieWpfApp.Interfaces.Services;
using MovieWpfApp.Interfaces.ViewModels;
using MovieWpfApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MovieWpfApp.ViewModels
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

        private IDialogCoordinator DialogCoordinator => GetService<IDialogCoordinator>()!;

        private IAsyncDialogService? DialogService => GetService<IAsyncDialogService>();

        public IEnvironmentService EnvironmentService => GetService<IEnvironmentService>()!;

        public ILogger Logger => GetService<ILogger>()!;

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

        protected override async void OnError(Exception ex, [CallerMemberName] string? callerName = null)
        {
            base.OnError(ex, callerName);
#pragma warning disable IDE0079
#pragma warning disable CA2254
            Logger.LogError(ex, Loc.An_error_has_occurred_in__Caller____Exception_, callerName, ex.Message);
#pragma warning restore CA2254
#pragma warning restore IDE0079
            var dialogSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = Loc.OK,
                CancellationToken = CancellationToken.None,
            };
            await DialogCoordinator.ShowMessageAsync(this, Loc.Error, string.Format(Loc.An_error_has_occurred_in_Arg0_Arg1, callerName, ex.Message), MessageDialogStyle.Affirmative, dialogSettings).ConfigureAwait(false);
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(DialogCoordinator != null, $"{nameof(DialogCoordinator)} is null");
            Debug.Assert(DialogService != null, $"{nameof(DialogService)} is null");
            Debug.Assert(EnvironmentService != null, $"{nameof(EnvironmentService)} is null");
            Debug.Assert(Logger != null, $"{nameof(Logger)} is null");
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            Debug.Assert(ParentViewModel != null, $"{nameof(ParentViewModel)} is null");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");

            await ReloadMoviesAsync(cancellationToken);
        }

        protected override void OnInitializeInRuntime()
        {
            base.OnInitializeInRuntime();

            Movies = [];
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
