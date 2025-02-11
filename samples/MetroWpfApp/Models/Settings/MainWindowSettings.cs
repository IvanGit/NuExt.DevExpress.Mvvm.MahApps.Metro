﻿using DevExpress.Mvvm;

namespace MovieWpfApp.Models
{
    public sealed class MainWindowSettings : BindableSettings
    {
        public bool MoviesOpened
        {
            get { return GetProperty(() => MoviesOpened); }
            set { SetProperty(() => MoviesOpened, value); }
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            MoviesOpened = true;
        }
    }
}
