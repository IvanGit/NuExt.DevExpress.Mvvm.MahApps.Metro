using DevExpress.Mvvm;
using System.IO;

namespace MetroWpfApp.Services
{
    public sealed class EnvironmentService : EnvironmentServiceBase, IEnvironmentService
    {
        public EnvironmentService(string baseDirectory, string[] args) : base(baseDirectory, args)
        {
            ConfigDir = Path.Combine(BaseDirectory, "Config");
            IOUtils.CheckDirectory(ConfigDir, true);
            LogsDir = Path.Combine(WorkingDirectory, "Logs");
            IOUtils.CheckDirectory(LogsDir, true);
        }

        #region Properties

        public string ConfigDir { get; }

        public string LogsDir { get; }

        #endregion
    }
}
