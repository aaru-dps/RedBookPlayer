using Avalonia.Controls;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class SettingsWindow : Window
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public SettingsViewModel Settings => DataContext as SettingsViewModel;
    }
}