using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class MainWindow : ReactiveWindow<MainViewModel>
    {
        public ReactiveWindow<SettingsViewModel> SettingsWindow;

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
        public void MainWindow_Drop(object sender, DragEventArgs e) => ViewModel?.ExecuteLoadDragDrop(e);

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void OnClosing(object sender, CancelEventArgs e) => ViewModel?.ExecuteStop();

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void OnKeyDown(object sender, KeyEventArgs e) => ViewModel?.ExecuteKeyPress(e);

        #endregion
    }
}