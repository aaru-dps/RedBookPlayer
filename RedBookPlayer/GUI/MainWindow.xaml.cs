using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer.GUI
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

        /// <summary>
        /// Apply a custom theme to the player
        /// </summary>
        /// <param name="theme">Path to the theme under the themes directory</param>
        public static void ApplyTheme(string theme)
        {
            // If no theme path is provided, we can ignore
            if(string.IsNullOrWhiteSpace(theme))
                return;

            // If the theme name is "default", we assume the internal theme is used
            if(theme.Equals("default", StringComparison.CurrentCultureIgnoreCase))
            {
                Instance.ContentControl.Content = new PlayerView();
            }
            else
            {
                string themeDirectory = $"{Directory.GetCurrentDirectory()}/themes/{theme}";
                string xamlPath       = $"{themeDirectory}/view.xaml";

                if(!File.Exists(xamlPath))
                {
                    Console.WriteLine("Warning: specified theme doesn't exist, reverting to default");
                    return;
                }

                try
                {
                    string xaml = File.ReadAllText(xamlPath);
                    xaml = xaml.Replace("Source=\"", $"Source=\"file://{themeDirectory}/");
                    Instance.ContentControl.Content = new PlayerView(xaml);
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

        /// <summary>
        /// Initialize the main window
        /// </summary>
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

            AddHandler(DragDrop.DropEvent, MainWindow_Drop);
        }

        #region Event Handlers

        public async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            PlayerView playerView = ContentControl.Content as PlayerView;
            if(playerView == null)
                return;

            IEnumerable<string> fileNames = e.Data.GetFileNames();
            foreach(string filename in fileNames)
            {
                bool loaded = await playerView.LoadImage(filename);
                if(loaded)
                    break;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.F1)
            {
                settingsWindow = new SettingsWindow(App.Settings);
                settingsWindow.Show();
            }
        }

        #endregion
    }
}