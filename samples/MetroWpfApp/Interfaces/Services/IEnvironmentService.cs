namespace MetroWpfApp.Interfaces.Services
{
    /// <summary>
    /// Provides access to various directories related to the application's environment, 
    /// such as the base directory, configuration directory, and settings directory.
    /// </summary>
    public interface IEnvironmentService
    {
        /// <summary>
        /// Gets the base application directory.
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Gets the application configuration directory.
        /// </summary>
        string ConfigDirectory { get; }

        /// <summary>
        /// Gets the logs directory where application logs are stored.
        /// </summary>
        string LogsDirectory { get; }

        /// <summary>
        /// Gets the application settings directory.
        /// </summary>
        string SettingsDirectory { get; }
    }
}
