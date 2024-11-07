using MahApps.Metro.Controls.Dialogs;
using System.Diagnostics;
using System.Windows;

namespace DevExpress.Mvvm.UI
{
    /// <summary>
    /// Provides asynchronous methods to show and manage dialogs using MahApps.Metro dialog coordinator.
    /// Extends ViewServiceBase and implements IAsyncDialogService interface.
    /// </summary>
    public class MetroDialogService : ViewServiceBase, IAsyncDialogService
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="DialogCoordinator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DialogCoordinatorProperty = DependencyProperty.Register(
            nameof(DialogCoordinator), typeof(IDialogCoordinator), typeof(MetroDialogService));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the DialogCoordinator used to manage dialog interactions.
        /// </summary>
        public IDialogCoordinator? DialogCoordinator
        {
            get => (IDialogCoordinator)GetValue(DialogCoordinatorProperty);
            set => SetValue(DialogCoordinatorProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Displays a dialog asynchronously with the specified parameters.
        /// </summary>
        /// <param name="dialogCommands">A collection of UICommand objects representing the buttons available in the dialog.</param>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="documentType">The type of the view to display within the dialog.</param>
        /// <param name="viewModel">The ViewModel associated with the view.</param>
        /// <param name="parameter">Additional parameters for initializing the view.</param>
        /// <param name="parentViewModel">The parent ViewModel for context.</param>
        /// <param name="cancellationToken">A token to cancel the dialog operation if needed.</param>
        /// <returns>A ValueTask&lt;UICommand?&gt; representing the command selected by the user.</returns>
        public async ValueTask<UICommand?> ShowDialogAsync(IEnumerable<UICommand> dialogCommands, string? title, string? documentType, object? viewModel, object? parameter, object? parentViewModel, CancellationToken cancellationToken = default)
        {
            Debug.Assert(DialogCoordinator != null, $"{nameof(DialogCoordinator)} is null");

            var view = CreateAndInitializeView(documentType, viewModel, parameter, parentViewModel);

            var dialogSettings = new MetroDialogSettings { CancellationToken = cancellationToken };
            var dialog = new MetroDialog(dialogSettings)
            {
                Title = title,
                Content = view,
                CommandsSource = dialogCommands,
            };

            var dialogCoordinator = DialogCoordinator ?? MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
            await dialogCoordinator.ShowMetroDialogAsync(this, dialog);

            var result = await dialog.WaitForButtonPressAsync();

            // once a button as been clicked, begin removing the dialog.
            await dialogCoordinator.HideMetroDialogAsync(this, dialog);

            return result;
        }

        #endregion
    }
}
