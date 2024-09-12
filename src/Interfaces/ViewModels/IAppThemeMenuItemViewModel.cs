using System.Windows.Media;

namespace DevExpress.Mvvm
{
    public interface IAccentColorMenuItemViewModel : IMenuItemViewModel
    {
        Brush? BorderColorBrush { get; }
        Brush? ColorBrush { get; }
    }

    public interface IAppThemeMenuItemViewModel : IAccentColorMenuItemViewModel
    {
    }
}
