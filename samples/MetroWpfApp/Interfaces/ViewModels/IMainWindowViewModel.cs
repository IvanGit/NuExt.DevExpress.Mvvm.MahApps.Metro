using DevExpress.Mvvm;

namespace MetroWpfApp.Interfaces.ViewModels
{
    public interface IMainWindowViewModel
    {
        IAsyncCommand? CloseMovieCommand { get; }
        IAsyncCommand? OpenMovieCommand { get; }
    }
}
