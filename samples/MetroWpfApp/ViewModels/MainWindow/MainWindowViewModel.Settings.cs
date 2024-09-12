using MetroWpfApp.Models;
using System.Diagnostics;

namespace MetroWpfApp.ViewModels
{
    internal partial class MainWindowViewModel
    {
        #region Properties

        public MainWindowSettings Settings
        {
            get { return GetProperty(() => Settings); }
            set { SetProperty(() => Settings, value); }
        }

        #endregion

        #region Methods

        private void CreateSettings()
        {
            Settings = new MainWindowSettings();
            Settings.Initialize();
            Settings.SuspendChanges();
            Lifetime.Add(SaveSettings);
        }

        private bool LoadSettings()
        {
            Debug.Assert(IsInitialized);
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings.IsSuspended)
            {
                Settings.ResumeChanges();
                Debug.Assert(Settings.IsSuspended == false);
                using (Settings.SuspendDirty())
                {
                    return SettingsService?.LoadSettings(Settings) == true;
                }
            }
            return false;
        }

        private void SaveSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings.IsDirty)
            {
                if (SettingsService?.SaveSettings(Settings) == true)
                {
                    Settings.ResetDirty();
                }
            }
        }

        #endregion

    }
}
