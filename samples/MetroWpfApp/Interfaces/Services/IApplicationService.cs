using DevExpress.Mvvm;
using MetroWpfApp.Models;

namespace MetroWpfApp.Interfaces.Services
{
    public interface IApplicationService: ISupportServices
    {
        AppSettings Settings { get; }
    }
}
