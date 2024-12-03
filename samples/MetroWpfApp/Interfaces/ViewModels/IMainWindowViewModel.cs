using DevExpress.Mvvm;

namespace MovieWpfApp.Interfaces.ViewModels
{
    public interface IMainWindowViewModel
    {
        IAsyncCommand? CloseMovieCommand { get; }
        IAsyncCommand? OpenMovieCommand { get; }
    }
}
