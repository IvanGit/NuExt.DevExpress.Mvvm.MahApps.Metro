﻿using DevExpress.Mvvm;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MahApps.Metro.Controls
{
    public sealed class MenuItemStyleSelector : StyleSelector
    {
        public Style? AccentColorMenuItemStyle { get; set; }

        public Style? AppThemeMenuItemStyle { get; set; }

        public Style? DefaultMenuItemStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            Debug.Assert(container is FrameworkElement);
            return item switch
            {
                IAppThemeMenuItemViewModel => AppThemeMenuItemStyle,
                IAccentColorMenuItemViewModel => AccentColorMenuItemStyle,
                _ => DefaultMenuItemStyle
            } ?? throw new ArgumentNullException(nameof(item));
        }
    }
}
