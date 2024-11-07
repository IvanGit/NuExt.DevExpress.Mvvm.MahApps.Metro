using DevExpress.Mvvm;
using MetroWpfApp.Models;
using System.Diagnostics;
using MetroWpfApp.Interfaces.Services;

namespace MetroWpfApp.ViewModels
{
    internal sealed class EditMovieViewModel: ControlViewModel
    {
        #region Properties

        public MovieModel Movie => (MovieModel)Parameter!;

        #endregion

        #region Services

        public IMoviesService MoviesService => GetService<IMoviesService>()!;

        #endregion

        #region Methods

        protected override ValueTask OnInitializeAsync(CancellationToken cancellationToken)
        {
            Debug.Assert(Movie != null, $"{nameof(Movie)} is null");
            Debug.Assert(MoviesService != null, $"{nameof(MoviesService)} is null");
            return default;
        }

        #endregion
    }
}
