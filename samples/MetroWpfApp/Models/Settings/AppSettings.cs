using DevExpress.Mvvm;

namespace MovieWpfApp.Models
{
    public sealed class AppSettings : BindableSettings
    {
        public string? AppTheme
        {
            get { return GetProperty(() => AppTheme); }
            set { SetProperty(() => AppTheme, value); }
        }
    }
}
