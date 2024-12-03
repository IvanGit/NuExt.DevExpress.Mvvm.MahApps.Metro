using DevExpress.Mvvm;
using MovieWpfApp.Interfaces.Services;
using MovieWpfApp.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels
{
    internal sealed class EditMovieViewModel : ControlViewModel, IDataErrorInfo
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

        #region IDataErrorInfo

        public string Error => Movie.Error;

        string IDataErrorInfo.this[string columnName] => null!;

        #endregion
    }
}
