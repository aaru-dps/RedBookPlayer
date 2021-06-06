using System;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer
{
    public class MainWindow : Window
    {
        public static MainWindow     Instance;
        public        ContentControl ContentControl;
        public        Window         settingsWindow;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        public static void ApplyTheme(string theme)
        {
            if((theme ?? "") == "")
            {
                return;
            }

            if(theme == "default")
            {
                Instance.ContentControl.Content = new PlayerView();
            }
            else
            {
                string themeDirectory = Directory.GetCurrentDirectory() + "/themes/" + theme;
                string xamlPath       = themeDirectory                  + "/view.xaml";

                if(!File.Exists(xamlPath))
                {
                    Console.WriteLine("Warning: specified theme doesn't exist, reverting to default");

                    return;
                }

                try
                {
                    Instance.ContentControl.Content =
                        new PlayerView(File.ReadAllText(xamlPath).
                                            Replace("Source=\"", $"Source=\"file://{themeDirectory}/"));
                }
                catch(XmlException ex)
                {
                    Console.WriteLine($"Error: invalid theme XAML ({ex.Message}), reverting to default");
                    Instance.ContentControl.Content = new PlayerView();
                }
            }

            Instance.Width  = ((PlayerView)Instance.ContentControl.Content).Width;
            Instance.Height = ((PlayerView)Instance.ContentControl.Content).Height;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.F1)
            {
                settingsWindow = new SettingsWindow(App.Settings);
                settingsWindow.Show();
            }
        }

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            ContentControl         = this.FindControl<ContentControl>("Content");
            ContentControl.Content = new PlayerView();

            Instance.MaxWidth  = ((PlayerView)Instance.ContentControl.Content).Width;
            Instance.MaxHeight = ((PlayerView)Instance.ContentControl.Content).Height;

            ContentControl.Content = new PlayerView();

            CanResize = false;

            KeyDown += OnKeyDown;

            Closing += (s, e) =>
            {
                settingsWindow?.Close();
                settingsWindow = null;
            };

            Closing += (e, f) =>
            {
                PlayerView.Player.Stop();
            };
        }
    }
}