using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;
using RedBookPlayer.Models;

namespace RedBookPlayer.GUI.Views
{
    public class SettingsWindow : Window
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public SettingsViewModel Settings => DataContext as SettingsViewModel;

        private string _selectedTheme;

        public SettingsWindow() {}

        public SettingsWindow(SettingsViewModel settings)
        {
            DataContext = settings;
            InitializeComponent();
        }

        void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        #region Event Handlers

        public void ApplySettings(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(_selectedTheme))
            {
                Settings.SelectedTheme = _selectedTheme;
                App.MainWindow.PlayerView?.PlayerViewModel?.ApplyTheme(_selectedTheme);
            }

            SaveDiscValues();
            SaveKeyboardList();
            Settings.Save();
        }

        public void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0)
                return;

            _selectedTheme = (string)e.AddedItems[0];
        }

        public void VolumeChanged(object s, AvaloniaPropertyChangedEventArgs e)
        {
            try
            {
                TextBlock volumeLabel = this.FindControl<TextBlock>("VolumeLabel");
                if(volumeLabel != null)
                    volumeLabel.Text = Settings.Volume.ToString();
            }
            catch { }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Save back the disc enum values
        /// </summary>
        private void SaveDiscValues()
        {
            Settings.DataPlayback      = (DataPlayback)this.FindControl<ComboBox>("DataPlayback").SelectedItem;
            Settings.SessionHandling   = (SessionHandling)this.FindControl<ComboBox>("SessionHandling").SelectedItem;
        }

        /// <summary>
        /// Save back all values from keyboard bindings
        /// </summary>
        private void SaveKeyboardList()
        {
            Settings.LoadImageKey              = (Key)this.FindControl<ComboBox>("LoadImageKeyBind").SelectedItem;
            Settings.TogglePlaybackKey         = (Key)this.FindControl<ComboBox>("TogglePlaybackKeyBind").SelectedItem;
            Settings.StopPlaybackKey           = (Key)this.FindControl<ComboBox>("StopPlaybackKeyBind").SelectedItem;
            Settings.EjectKey                  = (Key)this.FindControl<ComboBox>("EjectKeyBind").SelectedItem;
            Settings.NextTrackKey              = (Key)this.FindControl<ComboBox>("NextTrackKeyBind").SelectedItem;
            Settings.PreviousTrackKey          = (Key)this.FindControl<ComboBox>("PreviousTrackKeyBind").SelectedItem;
            Settings.NextIndexKey              = (Key)this.FindControl<ComboBox>("NextIndexKeyBind").SelectedItem;
            Settings.PreviousIndexKey          = (Key)this.FindControl<ComboBox>("PreviousIndexKeyBind").SelectedItem;
            Settings.FastForwardPlaybackKey    = (Key)this.FindControl<ComboBox>("FastForwardPlaybackKeyBind").SelectedItem;
            Settings.RewindPlaybackKey         = (Key)this.FindControl<ComboBox>("RewindPlaybackKeyBind").SelectedItem;
            Settings.VolumeUpKey               = (Key)this.FindControl<ComboBox>("VolumeUpKeyBind").SelectedItem;
            Settings.VolumeDownKey             = (Key)this.FindControl<ComboBox>("VolumeDownKeyBind").SelectedItem;
            Settings.ToggleMuteKey             = (Key)this.FindControl<ComboBox>("ToggleMuteKeyBind").SelectedItem;
            Settings.ToggleDeEmphasisKey       = (Key)this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind").SelectedItem;
        }

        #endregion
    }
}