using Avalonia.Controls;
using Avalonia.Interactivity;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class SettingsWindow : Window
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public SettingsViewModel Settings => DataContext as SettingsViewModel;

        #region Event Handlers

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void ApplyButton_Click(object sender, RoutedEventArgs e) => Settings?.ExecuteApplySettings();

        #endregion
    }
}