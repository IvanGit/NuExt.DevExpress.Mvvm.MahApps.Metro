using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieWpfApp.Interfaces.Services;
using MovieWpfApp.Models;
using MovieWpfApp.Services;
using MovieWpfApp.ViewModels;
using NLog;
using NLog.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MovieWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public sealed partial class App : IApplicationService, INotifyPropertyChanged, IDispatcherObject
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly bool _createdNew;
        private readonly EventWaitHandle _ewh;
        private readonly AsyncLifetime _lifetime = new(continueOnCapturedContext: true);

        public App()
        {
            _ewh = new EventWaitHandle(false, EventResetMode.AutoReset, $"{GetType().FullName}", out _createdNew);
            _lifetime.AddDisposable(_ewh);
            _lifetime.Add(_cts.Cancel);
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        #region Properties

        public PerformanceMonitor PerformanceMonitor { get; } = new(Process.GetCurrentProcess(), CultureInfo.InvariantCulture)
        {
            ShowPeakMemoryUsage = true,
            ShowManagedMemory = true,
            ShowPeakManagedMemory = true,
            ShowThreads = true
        };

        public IServiceContainer ServiceContainer => DevExpress.Mvvm.ServiceContainer.Default;

        public AppSettings Settings { get; } = new();

        #endregion

        #region Services

        public IEnvironmentService? EnvironmentService
        {
            get => GetService<IEnvironmentService>();
            private set
            {
                var environmentService = EnvironmentService;//trick for WPF PropertyChanged subscription
                if (environmentService != null)
                {
                    ServiceContainer.UnregisterService(environmentService);
                }
                ServiceContainer.RegisterService(value);
                OnPropertyChanged();
            }
        }

        private IOpenWindowsService? OpenWindowsService => GetService<IOpenWindowsService>();

        private ISettingsService? SettingsService => GetService<ISettingsService>();

        #endregion

        #region Event Handlers

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = GetService<ILogger>();
            if (logger?.IsEnabled(LogLevel.Error) == true)
            {
                logger.LogError(e.Exception, "Dispatcher Unhandled Exception: {Exception}.", e.Exception.Message);
            }
            e.Handled = true;
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            var logger = GetService<ILogger>();
            try
            {
                await _lifetime.DisposeAsync();
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                {
                    logger.LogError(ex, "Application Exit Exception: {Exception}.", ex.Message);
                }
                Debug.Assert(false, ex.Message);
            }

            if (logger?.IsEnabled(LogLevel.Information) == true)
            {
                logger.LogInformation("Application exited with code {ExitCode}.", e.ApplicationExitCode);
            }
            LogManager.Shutdown();
        }

        private async void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
        {
            if (e.ReasonSessionEnding != ReasonSessionEnding.Shutdown)
            {
                return;
            }
            var logger = GetService<ILogger>();
            try
            {
                await OpenWindowsService!.DisposeAsync();
            }
            catch (Exception ex)
            {
                if (logger?.IsEnabled(LogLevel.Error) == true)
                {
                    logger.LogError(ex, "Application SessionEnding Exception: {Exception}.", ex.Message);
                }
            }
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!_createdNew)
            {
                _ewh.Set();
                Shutdown();
                return;
            }

            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorTraceListener());

            EnvironmentService = new EnvironmentService(AppDomain.CurrentDomain.BaseDirectory, e.Args);
            //https://docs.devexpress.com/WPF/17444/mvvm-framework/services/getting-started
            ServiceContainer.RegisterService(new DispatcherService() { Name = "AppDispatcherService" });
            ServiceContainer.RegisterService(this);
            //ServiceContainer.RegisterService(new OpenWindowsService());
            _lifetime.AddAsyncDisposable(OpenWindowsService!);

            var configuration = BuildConfiguration(EnvironmentService);
            ServiceContainer.RegisterService(configuration);
            ConfigureLogging(EnvironmentService);

            EnvironmentService.LoadLocalization(typeof(Loc), CultureInfo.CurrentUICulture.IetfLanguageTag);

            InitializeSettings();
            InitializeAppTheme(configuration);

            var logger = GetService<ILogger>();
            logger?.LogInformation("Application started.");

            ServiceContainer.RegisterService(new MoviesService(Path.Combine(EnvironmentService.BaseDirectory, "movies.json")));

            var viewModel = new MainWindowViewModel();
            try
            {
                var window = new MainWindow { DataContext = viewModel };
                await viewModel.SetParentViewModel(this).InitializeAsync(viewModel.CancellationTokenSource.Token);
                window.Show();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error while initialization");
                Debug.Assert(false, ex.Message);
                await viewModel.DisposeAsync();
                Shutdown();
                return;
            }

            _ = Task.Run(() => WaitForNotifyAsync(_cts.Token), _cts.Token);
            _ = Task.Run(() => PerformanceMonitor.RunAsync(_cts.Token), _cts.Token);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            var logger = GetService<ILogger>();
            if (logger?.IsEnabled(LogLevel.Error) == true)
            {
                logger.LogError(e.Exception, "TaskScheduler Unobserved Task Exception: {Exception}.", e.Exception.Message);
            }
        }

        #endregion

        #region Methods

        private static IConfiguration BuildConfiguration(IEnvironmentService environmentService)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(environmentService.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
            return configuration;
        }

        private void ConfigureLogging(IEnvironmentService environmentService)
        {
            Debug.Assert(IOUtils.NormalizedPathEquals(environmentService.BaseDirectory, Directory.GetCurrentDirectory()));
            var configFile = Path.Combine(environmentService.ConfigDirectory, "nlog.config.json");
            Debug.Assert(File.Exists(configFile), $"Configuration file not found: {configFile}");
            var config = new ConfigurationBuilder()
                .SetBasePath(environmentService.BaseDirectory)
                .AddJsonFile(configFile, optional: false, reloadOnChange: true)
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
#if DEBUG
                builder.SetMinimumLevel(LogLevel.Debug);
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
                builder.AddNLog(config);
            });
            LogManager.Configuration!.Variables["basedir"] = environmentService.LogsDirectory;
            ServiceContainer.RegisterService(loggerFactory);

            var logger = loggerFactory.CreateLogger("App");
            Debug.Assert(logger.IsEnabled(LogLevel.Debug));
            ServiceContainer.RegisterService(logger);
        }

        public T? GetService<T>() where T : class
        {
            return ServiceContainer.GetService<T>();
        }

        private void InitializeAppTheme(IConfiguration configuration)
        {
            ControlzEx.Theming.ThemeManager.Current.ChangeTheme(this, Settings.AppTheme ?? configuration["DefaultAppTheme"] ?? "Dark.Cyan");
            void OnCurrentThemeChanged(object? s, ControlzEx.Theming.ThemeChangedEventArgs args)
            {
                Settings.AppTheme = args.NewTheme.Name;
            }
            ControlzEx.Theming.ThemeManager.Current.ThemeChanged += OnCurrentThemeChanged;
        }

        private void InitializeSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            Settings.Initialize();
            _lifetime.AddBracket(LoadSettings, SaveSettings);
        }

        private void LoadSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            Debug.Assert(Settings.IsSuspended == false);
            using (Settings.SuspendDirty())
            {
                SettingsService?.LoadSettings(Settings);
            }
        }

        private void SaveSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings.IsDirty && SettingsService!.SaveSettings(Settings))
            {
                Settings.ResetDirty();
            }
        }

        private async Task WaitForNotifyAsync(CancellationToken cancellationToken)
        {
            using var awaiter = new AsyncWaitHandle(_ewh);
            try
            {
                while (await awaiter.WaitOneAsync(cancellationToken))//_ewh is set
                {
                    await this.InvokeAsync(() =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        MainWindow?.BringToFront();
                    });
                }
            }
            catch (OperationCanceledException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                var logger = GetService<ILogger>();
                logger?.LogError(ex, "Unexpected error");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            Debug.Assert(PropertyChanged != null);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
