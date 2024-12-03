using MovieWpfApp.Models;
using System.Diagnostics;

namespace MovieWpfApp.ViewModels
{
    internal partial class MainWindowViewModel
    {
        #region Properties

        public MainWindowSettings? Settings
        {
            get { return GetProperty(() => Settings); }
            set { SetProperty(() => Settings, value); }
        }

        #endregion

        #region Methods

        private void CreateSettings()
        {
            if (Settings != null)
            {
                return;
            }
            Settings = new MainWindowSettings();
            Settings.Initialize();
            Lifetime.AddBracket(LoadSettings, SaveSettings);
        }

        private void LoadSettings()
        {
            Debug.Assert(IsInitialized, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({GetHashCode()}) is not initialized.");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
            using (Settings!.SuspendDirty())
            {
                SettingsService!.LoadSettings(Settings);
            }
        }

        private void SaveSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            Debug.Assert(Settings != null, $"{nameof(Settings)} is null");
            if (Settings!.IsDirty && SettingsService!.SaveSettings(Settings))
            {
                Settings.ResetDirty();
            }
        }

        #endregion

    }
}
