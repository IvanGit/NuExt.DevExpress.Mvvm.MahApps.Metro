﻿using DevExpress.Mvvm;
using MahApps.Metro.Controls.Dialogs;
using MovieWpfApp.Models;
using MovieWpfApp.Views;
using System.Windows.Input;

namespace MovieWpfApp.ViewModels
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

        public ICommand? ExpandOrCollapseCommand
        {
            get => GetProperty(() => ExpandOrCollapseCommand);
            private set { SetProperty(() => ExpandOrCollapseCommand, value); }
        }

        public ICommand? MoveCommand
        {
            get => GetProperty(() => MoveCommand);
            private set { SetProperty(() => MoveCommand, value); }
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

        #endregion

        #region Command Methods

        private bool CanDelete() => CanEdit() && ParentViewModel?.CloseMovieCommand is not null;

        private async Task DeleteAsync()
        {
            var cancellationToken = GetCurrentCancellationToken();

            var dialogSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = Loc.Yes,
                NegativeButtonText = Loc.No,
                CancellationToken = cancellationToken
            };

            var dialogResult = await DialogCoordinator!.ShowMessageAsync(this, Loc.Confirmation,
                string.Format(Loc.Are_you_sure_you_want_to_delete__Arg0__ ,SelectedItem?.Name),
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
                    await ParentViewModel!.CloseMovieCommand!.ExecuteAsync(movie);
                }
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies!.FindByPath(parentPath);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanEdit()
        {
            return IsUsable && SelectedItem?.IsEditable == true && DialogCoordinator != null && DialogService != null;
        }

        private async Task EditAsync()
        {
            var cancellationToken = GetCurrentCancellationToken();

            var clone = SelectedItem!.Clone();

            switch (clone)
            {
                case MovieGroupModel group:
                    var dialogSettings = new MetroDialogSettings
                    {
                        AffirmativeButtonText = Loc.OK,
                        NegativeButtonText = Loc.Cancel,
                        CancellationToken = cancellationToken,
                        DefaultText = group.Name,
                    };
                    var groupName = await DialogCoordinator!.ShowInputAsync(this, Loc.Edit_Group,
                        Loc.Enter_new_group_name, dialogSettings);
                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        return;
                    }
                    group.Name = groupName!;
                    break;
                case MovieModel movie:

                    await using (var viewModel = new EditMovieViewModel())
                    {
                        await viewModel.SetParameter(movie).SetParentViewModel(this).InitializeAsync(cancellationToken);
                        var dlgResult = await DialogService!.ShowDialogAsync(MessageButton.OKCancel, Loc.Edit_Movie,
                            nameof(EditMovieView), viewModel, cancellationToken);
                        if (dlgResult != MessageResult.OK) 
                        {
                            return;
                        }
                    }
                    break;
            }

            var path = clone.GetPath();
            bool result = await MoviesService.SaveAsync(SelectedItem, clone, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies!.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanNewGroup()
        {
            return IsUsable && SelectedItem is MovieGroupModel { IsLost: false } && DialogCoordinator != null && DialogService != null;
        }

        private async Task NewGroupAsync()
        {
            var cancellationToken = GetCurrentCancellationToken();

            var dialogSettings = new MetroDialogSettings 
            {
                AffirmativeButtonText = Loc.OK,
                NegativeButtonText = Loc.Cancel,
                CancellationToken = cancellationToken,
                DefaultText = Loc.New_Group
            };

            var groupName = await DialogCoordinator!.ShowInputAsync(this, Loc.New_Group,
                Loc.Enter_new_group_name, dialogSettings);

            if (string.IsNullOrWhiteSpace(groupName))
            {
                return;
            }

            var model = new MovieGroupModel()
            {
                Name = groupName!,
                Parent = SelectedItem is MovieGroupModel { IsRoot: false } group ? group : null
            };
            var path = model.GetPath();

            bool result = await MoviesService.AddAsync(model, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies!.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanNewMovie() => CanNewGroup();

        private async Task NewMovieAsync()
        {
            var cancellationToken = GetCurrentCancellationToken();

            await using var viewModel = new EditMovieViewModel();

            var movie = new MovieModel()
            {
                Name = Loc.New_Movie,
                ReleaseDate = DateTime.Today,
                Parent = SelectedItem is MovieGroupModel { IsRoot: false } group ? group : null
            };

            await viewModel.SetParameter(movie).SetParentViewModel(this).InitializeAsync(cancellationToken);
            var dlgResult = await DialogService!.ShowDialogAsync(MessageButton.OKCancel, Loc.New_Movie, nameof(EditMovieView), viewModel, cancellationToken);
            if (dlgResult != MessageResult.OK)
            {
                return;
            }

            var path = viewModel.Movie.GetPath();
            bool result = await MoviesService.AddAsync(viewModel.Movie, cancellationToken);
            if (result)
            {
                await ReloadMoviesAsync(cancellationToken);
                var item = Movies!.FindByPath(path);
                //item?.Expand();
                SelectedItem = item;
            }
        }

        private bool CanOpenMovie(MovieModelBase? item)
        {
            return IsUsable && item is MovieModel && ParentViewModel?.OpenMovieCommand is not null;
        }

        private async Task OpenMovieAsync(MovieModelBase? item)
        {
            await ParentViewModel!.OpenMovieCommand!.ExecuteAsync(item as MovieModel);
        }

        private bool CanMove(MovieModelBase? draggedObject)
        {
            return IsUsable && draggedObject?.CanDrag == true;
        }

        private async Task MoveAsync(MovieModelBase draggedObject)
        {
            var cancellationToken = GetCurrentCancellationToken();

            var path = draggedObject.GetPath();
            await ReloadMoviesAsync(cancellationToken);
            var item = Movies!.FindByPath(path);
            //item?.Expand();
            SelectedItem = item;
        }

        private void ExpandOrCollapse(bool expand)
        {
            if (expand)
            {
                Movies!.OfType<MovieGroupModel>().ForEach(m => m.ExpandAll());
            }
            else
            {
                Movies!.OfType<MovieGroupModel>().ForEach(m => m.CollapseAll());
            }
        }

        #endregion

        #region Methods

        protected override void CreateCommands()
        {
            base.CreateCommands();
            DeleteCommand = RegisterAsyncCommand(DeleteAsync, CanDelete);
            EditCommand = RegisterAsyncCommand(EditAsync, CanEdit);
            NewGroupCommand = RegisterAsyncCommand(NewGroupAsync, CanNewGroup);
            NewMovieCommand = RegisterAsyncCommand(NewMovieAsync, CanNewMovie);
            MoveCommand = RegisterAsyncCommand<MovieModelBase>(MoveAsync, CanMove);
            OpenMovieCommand = RegisterAsyncCommand<MovieModelBase?>(OpenMovieAsync, CanOpenMovie);
            ExpandOrCollapseCommand = RegisterCommand<bool>(ExpandOrCollapse, _ => IsUsable);
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            CreateSettings();
            if (!string.IsNullOrEmpty(Settings!.SelectedPath))
            {
                SelectedItem = Movies?.FindByPath(Settings.SelectedPath);
            }
        }

        #endregion
    }
}
