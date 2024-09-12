﻿using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page,
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
    //(used if a resource is not found in the page,
    // app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsPrefix("http://schemas.devexpress.com/winfx/2008/xaml/mvvm", "dxmvvm")]
[assembly: XmlnsDefinition("http://schemas.devexpress.com/winfx/2008/xaml/mvvm", "DevExpress.Mvvm")]
[assembly: XmlnsDefinition("http://schemas.devexpress.com/winfx/2008/xaml/mvvm", "DevExpress.Mvvm.UI")]

[assembly: XmlnsPrefix("http://metro.mahapps.com/winfx/xaml/controls", "mah")]
[assembly: XmlnsDefinition("http://metro.mahapps.com/winfx/xaml/controls", "MahApps.Metro.Controls")]
[assembly: XmlnsDefinition("http://metro.mahapps.com/winfx/xaml/controls", "MahApps.Metro.Controls.Dialogs")]