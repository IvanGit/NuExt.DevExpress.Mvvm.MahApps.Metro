using DevExpress.Mvvm;
using MovieWpfApp.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels
{
    internal sealed partial class MovieViewModel : DocumentContentViewModelBase
    {
        #region Properties

        public MovieModel Movie => (MovieModel)Parameter!;

        #endregion

        #region Event Handlers

        private void Movie_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(MovieModel.Name) or nameof(MovieModel.ReleaseDate))
            {
                UpdateTitle();
            }
        }

        #endregion

        #region Methods

        protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(Parameter is MovieModel);
            Lifetime.AddBracket(() => Movie.PropertyChanged += Movie_PropertyChanged,
                () => Movie.PropertyChanged -= Movie_PropertyChanged);
            UpdateTitle();
            return default;
        }

        private void UpdateTitle()
        {
            Title = $"{Movie.Name} [{Movie.ReleaseDate:yyyy}]";
        }

        #endregion
    }
}
