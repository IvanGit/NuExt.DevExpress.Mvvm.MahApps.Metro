using MetroWpfApp.Models;
using System.Diagnostics;

namespace MetroWpfApp.ViewModels
{
    internal partial class MoviesViewModel
    {
        #region Properties

        public MoviesSettings Settings
        {
            get { return GetProperty(() => Settings); }
            set { SetProperty(() => Settings, value); }
        }

        #endregion

        #region Methods

        private void CreateSettings()
        {
            Settings = new MoviesSettings();
            Settings.Initialize();
            Settings.SuspendChanges();
            Lifetime.Add(SaveSettings);
        }

        private bool LoadSettings()
        {
            Debug.Assert(IsInitialized, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({GetHashCode()}) is not initialized.");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings.IsSuspended)
            {
                Settings.ResumeChanges();
                Debug.Assert(Settings.IsSuspended == false);
                using (Settings.SuspendDirty())
                {
                    return SettingsService!.LoadSettings(Settings);
                }
            }
            return false;
        }

        private void SaveSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings.IsDirty)
            {
                if (SettingsService!.SaveSettings(Settings))
                {
                    Settings.ResetDirty();
                }
            }
        }

        #endregion

    }
}
