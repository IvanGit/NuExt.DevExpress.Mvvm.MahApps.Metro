using System.Windows.Media;

namespace DevExpress.Mvvm
{
    public class AccentColorMenuItemViewModel : MenuItemViewModel, IAccentColorMenuItemViewModel
    {
        public Brush? BorderColorBrush { get; set; }

        public Brush? ColorBrush { get; set; }
    }

    public class AppThemeMenuItemViewModel : AccentColorMenuItemViewModel, IAppThemeMenuItemViewModel
    {

    }
}
