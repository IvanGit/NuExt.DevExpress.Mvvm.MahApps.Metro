using MetroWpfApp.Models;
using System.Diagnostics;

namespace MetroWpfApp.ViewModels
{
    internal partial class MoviesViewModel
    {
        #region Properties

        public MoviesSettings? Settings
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
            Settings = new MoviesSettings();
            Settings.Initialize();
            Settings.SuspendChanges();
            Lifetime.AddBracket(LoadSettings, SaveSettings);
        }

        private void LoadSettings()
        {
            Debug.Assert(IsInitialized, $"{GetType().FullName} ({DisplayName ?? "Unnamed"}) ({GetHashCode()}) is not initialized.");
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings!.IsSuspended)
            {
                Settings.ResumeChanges();
                Debug.Assert(Settings.IsSuspended == false);
                using (Settings.SuspendDirty())
                {
                    SettingsService!.LoadSettings(Settings);
                }
            }
        }

        private void SaveSettings()
        {
            Debug.Assert(SettingsService != null, $"{nameof(SettingsService)} is null");
            if (Settings!.IsDirty)
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
