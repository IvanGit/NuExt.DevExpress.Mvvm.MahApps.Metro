﻿using DevExpress.Mvvm;

namespace MetroWpfApp.Models
{
    public sealed class MoviesSettings: BindableSettings
    {
        public string? SelectedPath
        {
            get { return GetProperty(() => SelectedPath); }
            set { SetProperty(() => SelectedPath, value); }
        }
    }
}