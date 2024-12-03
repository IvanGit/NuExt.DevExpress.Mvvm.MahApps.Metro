using MovieWpfApp.Models;

namespace MovieWpfApp.Interfaces.Services
{
    public interface IMoviesService
    {
        List<PersonModel> Persons { get; }

        ValueTask<bool> AddAsync(MovieModelBase model, CancellationToken cancellationToken);
        ValueTask<bool> DeleteAsync(MovieModelBase model, CancellationToken cancellationToken);
        ValueTask<List<MovieModelBase>> GetMoviesAsync(CancellationToken cancellationToken);
        ValueTask InitializeAsync(CancellationToken cancellationToken);
        ValueTask<bool> SaveAsync(MovieModelBase original, MovieModelBase clone, CancellationToken cancellationToken);

    }
}
