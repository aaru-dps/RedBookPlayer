using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class SettingsWindow : Window
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public SettingsViewModel Settings => DataContext as SettingsViewModel;

        public SettingsWindow() {}

        public SettingsWindow(SettingsViewModel settings)
        {
            DataContext = settings;
            InitializeComponent();
        }

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        #region Event Handlers

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void ApplyButton_Click(object sender, RoutedEventArgs e) => Settings?.ExecuteApplySettings();

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e) => Settings?.ExecuteThemeChanged(e);

        /// <remarks>
        /// This can't be set in the XAML because the current version of Avalonia emits XAML errors if it's set there directly
        /// </remarks>
        public void VolumeChanged(object s, AvaloniaPropertyChangedEventArgs e) => Settings?.ExecuteVolumeChanged(e);

        #endregion
    }
}