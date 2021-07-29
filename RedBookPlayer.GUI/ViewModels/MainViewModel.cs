using System.Reactive;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using RedBookPlayer.GUI.Views;

namespace RedBookPlayer.GUI.ViewModels
{
    public class MainViewModel
    {
        /// <summary>
        /// Read-only access to the control
        /// </summary>
        public ContentControl ContentControl => App.MainWindow.FindControl<ContentControl>("Content");

        /// <summary>
        /// Read-only access to the view
        /// </summary>
        public PlayerView PlayerView => ContentControl?.Content as PlayerView;

        #region Commands

        /// <summary>
        /// Command for handling keypresses
        /// </summary>
        public ReactiveCommand<KeyEventArgs, Unit> KeyPressCommand { get; }

        /// <summary>
        /// Command for loading a disc from drag and drop
        /// </summary>
        public ReactiveCommand<DragEventArgs, Unit> LoadDragDropCommand { get; }

        /// <summary>
        /// Command for stopping playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainViewModel()
        {
            KeyPressCommand = ReactiveCommand.Create<KeyEventArgs>(ExecuteKeyPress);
            LoadDragDropCommand = ReactiveCommand.Create<DragEventArgs>(ExecuteLoadDragDrop);
            StopCommand = ReactiveCommand.Create(ExecuteStop);
        }

        #region Helpers

        /// <summary>
        /// Execute the result of a keypress
        /// </summary>
        public void ExecuteKeyPress(KeyEventArgs e) => PlayerView?.ViewModel?.ExecuteKeyPress(e);

        /// <summary>
        /// Load the first valid drag-and-dropped disc image
        /// </summary>
        public void ExecuteLoadDragDrop(DragEventArgs e) => PlayerView?.ViewModel?.ExecuteLoadDragDrop(e);

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void ExecuteStop() => PlayerView?.ViewModel?.ExecuteStop();

        #endregion
    }
}
