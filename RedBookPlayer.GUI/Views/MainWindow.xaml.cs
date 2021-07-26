using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer.GUI.Views
{
    public class MainWindow : Window
    {
        public Window SettingsWindow;

        /// <summary>
        /// Read-only access to the control
        /// </summary>
        public ContentControl ContentControl => this.FindControl<ContentControl>("Content");

        /// <summary>
        /// Read-only access to the view
        /// </summary>
        public PlayerView PlayerView => App.MainWindow?.ContentControl?.Content as PlayerView;

        public MainWindow() => InitializeComponent();

        /// <summary>
        /// Initialize the main window
        /// </summary>
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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
        public void OnClosing(object sender, CancelEventArgs e) => PlayerView?.PlayerViewModel?.ExecuteStop();

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void OnKeyDown(object sender, KeyEventArgs e) => PlayerView?.PlayerViewModel?.ExecuteKeyPress(e);

        #endregion
    }
}