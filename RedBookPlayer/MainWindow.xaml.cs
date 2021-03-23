using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;

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

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                settingsWindow = new SettingsWindow();
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
                PlayerView.player.Shutdown();
            };
        }
    }
}