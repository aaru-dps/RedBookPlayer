using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class PlayerView : UserControl
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public PlayerViewModel PlayerViewModel => DataContext as PlayerViewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        public PlayerView()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new PlayerViewModel();
        }
    }
}