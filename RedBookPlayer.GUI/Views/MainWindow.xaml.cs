using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer.GUI.Views
{
    public class MainWindow : Window
    {
        public static MainWindow     Instance;
        public        Window         SettingsWindow;

        /// <summary>
        /// Read-only access to the control
        /// </summary>
        public ContentControl ContentControl => this.FindControl<ContentControl>("Content");

        /// <summary>
        /// Read-only access to the view
        /// </summary>
        public PlayerView PlayerView => Instance?.ContentControl?.Content as PlayerView;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the main window
        /// </summary>
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            PlayerView.PlayerViewModel.ApplyTheme(App.Settings.SelectedTheme);

            CanResize = false;

            // Add handlers
            Closing += (s, e) =>
            {
                SettingsWindow?.Close();
                SettingsWindow = null;
            };

            Closing += (e, f) =>
            {
                PlayerView?.PlayerViewModel?.ExecuteStop();
            };

            AddHandler(KeyDownEvent, OnKeyDown);
            AddHandler(DragDrop.DropEvent, MainWindow_Drop);
        }

        #region Event Handlers

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void MainWindow_Drop(object sender, DragEventArgs e) => PlayerView?.PlayerViewModel?.ExecuteLoadDragDrop(e);

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void OnKeyDown(object sender, KeyEventArgs e) => PlayerView?.PlayerViewModel?.ExecuteKeyPress(e);

        #endregion
    }
}