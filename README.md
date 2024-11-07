# NuExt.DevExpress.Mvvm.MahApps.Metro

`NuExt.DevExpress.Mvvm.MahApps.Metro` is a NuGet package that provides extensions for integrating [MahApps.Metro](https://github.com/MahApps/MahApps.Metro), a popular Metro-style UI toolkit for WPF applications, with the [DevExpress MVVM Framework](https://github.com/DevExpress/DevExpress.Mvvm.Free), a robust library designed to simplify and enhance the development of WPF applications using the Model-View-ViewModel (MVVM) pattern. This package includes services and components to facilitate the creation of modern, responsive, and visually appealing user interfaces using the MVVM pattern.

### Commonly Used Types

- **`DevExpress.Mvvm.UI.DialogCoordinatorService`**: Provides dialog coordination services using MahApps.Metro dialogs.
- **`DevExpress.Mvvm.UI.MetroDialogService`**: `IAsyncDialogService` implementation for Metro dialogs.
- **`DevExpress.Mvvm.UI.MetroTabbedDocumentUIService`**: Manages tabbed documents within a UI.
- **`MahApps.Metro.Controls.Dialogs.MetroDialog`**: The class used for custom dialogs.

### Key Features

The `MetroTabbedDocumentUIService` class is responsible for managing tabbed documents within a UI that utilizes the Metro design language. It extends the `DocumentUIServiceBase` and implements interfaces for asynchronous document management and disposal. This service allows for the creation, binding, and lifecycle management of tabbed documents within controls such as `MetroTabControl`, `UserControl`, and `Window`.

### Installation

You can install `NuExt.DevExpress.Mvvm.MahApps.Metro` via [NuGet](https://www.nuget.org/):

```sh
dotnet add package NuExt.DevExpress.Mvvm.MahApps.Metro
```

Or through the Visual Studio package manager:

1. Go to `Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution...`.
2. Search for `NuExt.DevExpress.Mvvm.MahApps.Metro`.
3. Click "Install".

### Usage Examples

For comprehensive examples of how to use the package, refer to the [samples](samples) directory in the repository. These samples illustrate best practices for using DevExpress MVVM and MahApps.Metro with these extensions.

### Contributing

Contributions are welcome! Feel free to submit issues, fork the repository, and send pull requests. Your feedback and suggestions for improvement are highly appreciated.

### License

Licensed under the MIT License. See the LICENSE file for details.