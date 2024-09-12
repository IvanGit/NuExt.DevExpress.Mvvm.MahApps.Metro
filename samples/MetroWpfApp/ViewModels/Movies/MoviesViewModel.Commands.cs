using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using MetroWpfApp.Models;
using MetroWpfApp.Views;
using System.Windows.Input;

namespace MetroWpfApp.ViewModels
{
    internal partial class MoviesViewModel
    {
        #region Commands

        public ICommand? DeleteCommand
        {
            get => GetProperty(() => DeleteCommand);
            private set { SetProperty(() => DeleteCommand, value); }
        }

        public ICommand? EditCommand
        {
            get => GetProperty(() => EditCommand);
            private set { SetProperty(() => EditCommand, value); }
        }

        public ICommand? ExpandCollapseCommand
        {
            get => GetProperty(() => ExpandCollapseCommand);
            private set { SetProperty(() => ExpandCollapseCommand, value); }
        }

        public ICommand? NewGroupCommand
        {
            get => GetProperty(() => NewGroupCommand);
            private set { SetProperty(() => NewGroupCommand, value); }
        }

        public ICommand? NewMovieCommand
        {
            get => GetProperty(() => NewMovieCommand);
            private set { SetProperty(() => NewMovieCommand, value); }
        }

        public ICommand? OpenMovieCommand
        {
            get => GetProperty(() => OpenMovieCommand);
            private set { SetProperty(() => OpenMovieCommand, value); }
        }

        public ICommand? MoveCommand
        {
            get => GetProperty(() => MoveCommand);
            private set { SetProperty(() => MoveCommand, value); }
        }

        #endregion

        #region Command Methods

        private bool CanDelete() => CanEdit();

        private async Task DeleteAsync()
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;

            var dialogSettings = new MetroDialogSettings
            {
                CancellationToken = cancellationToken,
                DefaultText = SelectedItem?.Name ?? string.Empty,
            };

            var dialogResult = await DialogCoordinator.ShowMessageAsync(this, "Confirmation",
                $"Are you sure you want to delete '{SelectedItem?.Name}'?",
                MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            if (dialogResult != MessageDialogResult.Affirmative)
            {
                return;
            }

            var itemToDelete = SelectedItem!;
            var parentPath = itemToDelete.Parent?.GetPath();
            bool result = await MoviesService.DeleteAsync(itemToDelete, cancellationToken);
            if (result)
            {
                if (itemToDelete is MovieModel movie)
                {
                    await ParentViewModel!.CloseMovieAsync(movie, cancellationToken);
                }
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies.FindByPath(parentPath);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanEdit()
        {
            if (!IsUsable)
            {
                return false;
            }
            return SelectedItem?.IsEditable == true;
        }

        private async Task EditAsync()
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;

            var clone = SelectedItem!.Clone();

            switch (clone)
            {
                case MovieGroupModel group:
                    var dialogSettings = new MetroDialogSettings
                    {
                        CancellationToken = cancellationToken,
                        DefaultText = group.Name,
                    };
                    var groupName = await DialogCoordinator.ShowInputAsync(this, "Edit Group Name",
                        "Enter new group name", dialogSettings);
                    if (string.IsNullOrWhiteSpace(groupName)) { return; }
                    group.Name = groupName!;
                    break;
                case MovieModel movie:

                    await using (var viewModel = new EditMovieViewModel())
                    {
                        await viewModel.SetParameter(movie).SetParentViewModel(this).InitializeAsync(cancellationToken);

                        var dlgResult = await DialogService.ShowDialogAsync(MessageButton.OKCancel, "Edit Movie",
                            nameof(EditMovieView), viewModel, cancellationToken);
                        if (dlgResult != MessageResult.OK) return;
                    }
                    break;
            }

            var path = clone.GetPath();
            bool result = await MoviesService.SaveAsync(SelectedItem, clone, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanNewGroup()
        {
            if (!IsUsable)
            {
                return false;
            }
            return SelectedItem is MovieGroupModel { IsLost: false };
        }

        private async Task NewGroupAsync()
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;

            var dialogSettings = new MetroDialogSettings { CancellationToken = cancellationToken };

            var groupName = await DialogCoordinator.ShowInputAsync(this, "New Group Name",
                "Enter new group name", dialogSettings);
            if (string.IsNullOrWhiteSpace(groupName)) { return; }

            var model = new MovieGroupModel()
            {
                Name = groupName!,
                Parent = SelectedItem as MovieGroupModel
            };
            var path = model.GetPath();

            bool result = await MoviesService.AddAsync(model, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanNewMovie() => CanNewGroup();

        private async Task NewMovieAsync()
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;

            await using var viewModel = new EditMovieViewModel();

            var movie = new MovieModel()
            {
                Name = "New Movie",
                ReleaseDate = DateTime.Today,
                Parent = SelectedItem as MovieGroupModel
            };

            await viewModel.SetParameter(movie).SetParentViewModel(this).InitializeAsync(cancellationToken);

            var dlgResult = await DialogService!.ShowDialogAsync(MessageButton.OKCancel, "New Movie", nameof(EditMovieView), viewModel, cancellationToken);
            if (dlgResult != MessageResult.OK) return;

            var path = viewModel.Movie.GetPath();
            bool result = await MoviesService.AddAsync(viewModel.Movie, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanOpenMovie(MovieModelBase? item)
        {
            if (!IsUsable)
            {
                return false;
            }
            return item is MovieModel && ParentViewModel is not null;
        }

        private async Task OpenMovieAsync(MovieModelBase? item)
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;
            await ParentViewModel!.OpenMovieAsync((item as MovieModel)!, cancellationToken);
        }

        private bool CanMove(MovieModelBase? draggedObject)
        {
            if (!IsUsable)
            {
                return false;
            }
            return draggedObject?.CanDrag == true;
        }

        private async Task MoveAsync(MovieModelBase? draggedObject)
        {
            var cancellationToken = GetAsyncCommand()?.CancellationTokenSource.Token ?? default;

            var path = draggedObject!.GetPath();
            await ReloadMoviesAsync(cancellationToken);
            var item = Movies.FindByPath(path);
            //item?.Expand();
            SelectedItem = item;
        }

        private void ExpandOrCollapse(bool expand)
        {
            if (expand)
            {
                Movies.OfType<MovieGroupModel>().ForEach(m => m.ExpandAll());
            }
            else
            {
                Movies.OfType<MovieGroupModel>().ForEach(m => m.CollapseAll());
            }
        }

        #endregion

        #region Methods

        private void CreateCommands()
        {
            DeleteCommand = RegisterAsyncCommand(DeleteAsync, CanDelete);
            EditCommand = RegisterAsyncCommand(EditAsync, CanEdit);
            NewGroupCommand = RegisterAsyncCommand(NewGroupAsync, CanNewGroup);
            NewMovieCommand = RegisterAsyncCommand(NewMovieAsync, CanNewMovie);
            MoveCommand = RegisterAsyncCommand<MovieModelBase?>(MoveAsync, CanMove);
            OpenMovieCommand = RegisterAsyncCommand<MovieModelBase?>(OpenMovieAsync, CanOpenMovie);
            ExpandCollapseCommand = RegisterCommand<bool>(ExpandOrCollapse, _ => IsUsable);
        }

        #endregion

    }
}
