using DevExpress.Mvvm;
using MovieWpfApp.Models;

namespace MovieWpfApp.Interfaces.Services
{
    public interface IApplicationService: ISupportServices
    {
        AppSettings Settings { get; }
    }
}
