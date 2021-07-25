using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RedBookPlayer.Models;

namespace RedBookPlayer.GUI.Views
{
    public class SettingsWindow : Window
    {
        private readonly Settings _settings;
        private          string _selectedTheme;
        private          ListBox _themeList;

        public SettingsWindow() {}

        public SettingsWindow(Settings settings)
        {
            DataContext = _settings = settings;
            InitializeComponent();
        }

        public void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            _selectedTheme = (string)e.AddedItems[0];
        }

        

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            PopulateThemes();

            this.FindControl<Slider>("VolumeSlider").PropertyChanged += (s, e) => UpdateView();
        }

        #region Event Handlers

        public void ApplySettings(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrWhiteSpace(_selectedTheme))
            {
                _settings.SelectedTheme = _selectedTheme;
                App.MainWindow.PlayerView?.PlayerViewModel?.ApplyTheme(_selectedTheme);
            }

            SaveDiscValues();
            SaveKeyboardList();
            _settings.Save();
        }

        public void UpdateView()
        {
            this.FindControl<TextBlock>("VolumeLabel").Text = _settings.Volume.ToString();
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Populate the list of themes
        /// </summary>
        private void PopulateThemes()
        {
            // Get a reference to the theme list
            _themeList = this.FindControl<ListBox>("ThemeList");
            _themeList.SelectionChanged += ThemeList_SelectionChanged;

            // Create a list of all found themes
            List<string> items = new List<string>();
            items.Add("default");

            // Ensure the theme directory exists
            if(!Directory.Exists("themes/"))
                Directory.CreateDirectory("themes/");

            // Add all theme directories if they're valid
            foreach(string dir in Directory.EnumerateDirectories("themes/"))
            {
                string themeName = dir.Split('/')[1];

                if(!File.Exists($"themes/{themeName}/view.xaml"))
                    continue;

                items.Add(themeName);
            }

            _themeList.Items = items;
        }

        /// <summary>
        /// Save back the disc enum values
        /// </summary>
        private void SaveDiscValues()
        {
            _settings.DataPlayback      = (DataPlayback)this.FindControl<ComboBox>("DataPlayback").SelectedItem;
            _settings.SessionHandling   = (SessionHandling)this.FindControl<ComboBox>("SessionHandling").SelectedItem;
        }

        /// <summary>
        /// Save back all values from keyboard bindings
        /// </summary>
        private void SaveKeyboardList()
        {
            _settings.LoadImageKey              = (Key)this.FindControl<ComboBox>("LoadImageKeyBind").SelectedItem;
            _settings.TogglePlaybackKey         = (Key)this.FindControl<ComboBox>("TogglePlaybackKeyBind").SelectedItem;
            _settings.StopPlaybackKey           = (Key)this.FindControl<ComboBox>("StopPlaybackKeyBind").SelectedItem;
            _settings.EjectKey                  = (Key)this.FindControl<ComboBox>("EjectKeyBind").SelectedItem;
            _settings.NextTrackKey              = (Key)this.FindControl<ComboBox>("NextTrackKeyBind").SelectedItem;
            _settings.PreviousTrackKey          = (Key)this.FindControl<ComboBox>("PreviousTrackKeyBind").SelectedItem;
            _settings.NextIndexKey              = (Key)this.FindControl<ComboBox>("NextIndexKeyBind").SelectedItem;
            _settings.PreviousIndexKey          = (Key)this.FindControl<ComboBox>("PreviousIndexKeyBind").SelectedItem;
            _settings.FastForwardPlaybackKey    = (Key)this.FindControl<ComboBox>("FastForwardPlaybackKeyBind").SelectedItem;
            _settings.RewindPlaybackKey         = (Key)this.FindControl<ComboBox>("RewindPlaybackKeyBind").SelectedItem;
            _settings.VolumeUpKey               = (Key)this.FindControl<ComboBox>("VolumeUpKeyBind").SelectedItem;
            _settings.VolumeDownKey             = (Key)this.FindControl<ComboBox>("VolumeDownKeyBind").SelectedItem;
            _settings.ToggleMuteKey             = (Key)this.FindControl<ComboBox>("ToggleMuteKeyBind").SelectedItem;
            _settings.ToggleDeEmphasisKey       = (Key)this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind").SelectedItem;
        }

        #endregion
    }
}