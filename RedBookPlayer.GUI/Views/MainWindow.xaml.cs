using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class MainWindow : ReactiveWindow<MainViewModel>
    {
        public MainWindow() => InitializeComponent();

        /// <summary>
        /// Initialize the main window
        /// </summary>
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            // Add handlers
            Closing += ViewModel.ExecuteStop;
            AddHandler(DragDrop.DropEvent, ViewModel.ExecuteLoadDragDrop);
            KeyDown += ViewModel.ExecuteKeyPress;
        }
    }
}