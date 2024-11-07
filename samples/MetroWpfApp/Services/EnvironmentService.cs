using DevExpress.Mvvm;
using MetroWpfApp.Interfaces.Services;
using System.IO;

namespace MetroWpfApp.Services
{
    internal sealed class EnvironmentService : EnvironmentServiceBase, IEnvironmentService
    {
        public EnvironmentService(string baseDirectory, params string[] args) : base(baseDirectory, Path.Combine(baseDirectory, "AppData"), args)
        {
        }
    }
}
