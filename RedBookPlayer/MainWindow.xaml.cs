using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using System;
using System.IO;

namespace RedBookPlayer
{
    public class MainWindow : Window
    {
        public static MainWindow Instance;
        public ContentControl ContentControl;
        public Window settingsWindow;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        public static void ApplyTheme(string theme)
        {
            if ((theme ?? "") == "")
            {
                return;
            }

            if (theme == "default")
            {
                MainWindow.Instance.ContentControl.Content = new PlayerView();
            }
            else
            {
                string themeDirectory = Directory.GetCurrentDirectory() + "/themes/" + theme;
                string xamlPath = themeDirectory + "/view.xaml";

                if (!File.Exists(xamlPath))
                {
                    Console.WriteLine($"Warning: specified theme doesn't exist, reverting to default");
                    return;
                }

                try
                {
                    MainWindow.Instance.ContentControl.Content = new PlayerView(
                        File.ReadAllText(xamlPath).Replace("Source=\"", $"Source=\"file://{themeDirectory}/")
                    );
                }
                catch (System.Xml.XmlException ex)
                {
                    Console.WriteLine($"Error: invalid theme XAML ({ex.Message}), reverting to default");
                    MainWindow.Instance.ContentControl.Content = new PlayerView();
                }
            }

            MainWindow.Instance.Width = ((PlayerView)MainWindow.Instance.ContentControl.Content).Width;
            MainWindow.Instance.Height = ((PlayerView)MainWindow.Instance.ContentControl.Content).Height;
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                settingsWindow = new SettingsWindow(App.Settings);
                settingsWindow.Show();
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            ContentControl = this.FindControl<ContentControl>("Content");
            ContentControl.Content = new PlayerView();

            MainWindow.Instance.MaxWidth = ((PlayerView)MainWindow.Instance.ContentControl.Content).Width;
            MainWindow.Instance.MaxHeight = ((PlayerView)MainWindow.Instance.ContentControl.Content).Height;

            ContentControl.Content = new PlayerView();

            this.CanResize = false;

            this.KeyDown += OnKeyDown;
            this.Closing += (s, e) =>
            {
                settingsWindow?.Close();
                settingsWindow = null;
            };

            this.Closing += (e, f) =>
            {
                PlayerView.Player.Shutdown();
            };
        }
    }
}