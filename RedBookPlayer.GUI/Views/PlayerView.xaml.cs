using Avalonia.Controls;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class PlayerView : UserControl
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public PlayerViewModel PlayerViewModel => DataContext as PlayerViewModel;
    }
}