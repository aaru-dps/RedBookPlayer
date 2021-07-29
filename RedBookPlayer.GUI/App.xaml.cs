using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;
using RedBookPlayer.GUI.Views;

namespace RedBookPlayer
{
    public class App : Application
    {
        public static MainWindow        MainWindow;
        public static SettingsViewModel Settings;

        static App() =>
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

        public override void Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += (e, f) =>
            {
                Console.WriteLine(((Exception)f.ExceptionObject).ToString());
            };

            Settings = SettingsViewModel.Load("settings.json");
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                MainWindow           = new MainWindow();
                desktop.MainWindow   = MainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

                MainWindow.PlayerView.ViewModel.ApplyTheme(Settings.SelectedTheme);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}