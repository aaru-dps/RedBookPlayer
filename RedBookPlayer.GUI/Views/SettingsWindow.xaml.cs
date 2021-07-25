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

        public void ApplySettings(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_selectedTheme))
            {
                _settings.SelectedTheme = _selectedTheme;
                App.MainWindow.PlayerView?.PlayerViewModel?.ApplyTheme(_selectedTheme);
            }

            SaveDiscValues();
            SaveKeyboardList();
            _settings.Save();
        }

        public void UpdateView() => this.FindControl<TextBlock>("VolumeLabel").Text = _settings.Volume.ToString();

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            PopulateThemes();
            PopulateDiscValues();
            PopulateKeyboardList();

            this.FindControl<Button>("ApplyButton").Click            += ApplySettings;
            this.FindControl<Slider>("VolumeSlider").PropertyChanged += (s, e) => UpdateView();
        }

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
        /// Populate the list of disc enum values
        /// </summary>
        private void PopulateDiscValues()
        {
            ComboBox dataPlayback = this.FindControl<ComboBox>("DataPlayback");
            ComboBox sessionHandling = this.FindControl<ComboBox>("SessionHandling");

            dataPlayback.Items = Enum.GetValues(typeof(DataPlayback));
            sessionHandling.Items = Enum.GetValues(typeof(SessionHandling));

            dataPlayback.SelectedItem = _settings.DataPlayback;
            sessionHandling.SelectedItem = _settings.SessionHandling;
        }

        /// <summary>
        /// Populate all of the keyboard bindings
        /// </summary>
        private void PopulateKeyboardList()
        {
            // Access all of the combo boxes
            ComboBox loadImageKeyBind = this.FindControl<ComboBox>("LoadImageKeyBind");
            ComboBox togglePlaybackKeyBind = this.FindControl<ComboBox>("TogglePlaybackKeyBind");
            ComboBox stopPlaybackKeyBind = this.FindControl<ComboBox>("StopPlaybackKeyBind");
            ComboBox ejectKeyBind = this.FindControl<ComboBox>("EjectKeyBind");
            ComboBox nextTrackKeyBind = this.FindControl<ComboBox>("NextTrackKeyBind");
            ComboBox previousTrackKeyBind = this.FindControl<ComboBox>("PreviousTrackKeyBind");
            ComboBox nextIndexKeyBind = this.FindControl<ComboBox>("NextIndexKeyBind");
            ComboBox previousIndexKeyBind = this.FindControl<ComboBox>("PreviousIndexKeyBind");
            ComboBox fastForwardPlaybackKeyBind = this.FindControl<ComboBox>("FastForwardPlaybackKeyBind");
            ComboBox rewindPlaybackKeyBind = this.FindControl<ComboBox>("RewindPlaybackKeyBind");
            ComboBox volumeUpKeyBind = this.FindControl<ComboBox>("VolumeUpKeyBind");
            ComboBox volumeDownKeyBind = this.FindControl<ComboBox>("VolumeDownKeyBind");
            ComboBox toggleMuteKeyBind = this.FindControl<ComboBox>("ToggleMuteKeyBind");
            ComboBox toggleDeEmphasisKeyBind = this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind");

            // Assign the list of values to all of them
            Array keyboardList = GenerateKeyboardList();
            loadImageKeyBind.Items = keyboardList;
            togglePlaybackKeyBind.Items = keyboardList;
            stopPlaybackKeyBind.Items = keyboardList;
            ejectKeyBind.Items = keyboardList;
            nextTrackKeyBind.Items = keyboardList;
            previousTrackKeyBind.Items = keyboardList;
            nextIndexKeyBind.Items = keyboardList;
            previousIndexKeyBind.Items = keyboardList;
            fastForwardPlaybackKeyBind.Items = keyboardList;
            rewindPlaybackKeyBind.Items = keyboardList;
            volumeUpKeyBind.Items = keyboardList;
            volumeDownKeyBind.Items = keyboardList;
            toggleMuteKeyBind.Items = keyboardList;
            toggleDeEmphasisKeyBind.Items = keyboardList;

            // Set all of the currently selected items
            loadImageKeyBind.SelectedItem = _settings.LoadImageKey;
            togglePlaybackKeyBind.SelectedItem = _settings.TogglePlaybackKey;
            stopPlaybackKeyBind.SelectedItem = _settings.StopPlaybackKey;
            ejectKeyBind.SelectedItem = _settings.EjectKey;
            nextTrackKeyBind.SelectedItem = _settings.NextTrackKey;
            previousTrackKeyBind.SelectedItem = _settings.PreviousTrackKey;
            nextIndexKeyBind.SelectedItem = _settings.NextIndexKey;
            previousIndexKeyBind.SelectedItem = _settings.PreviousIndexKey;
            fastForwardPlaybackKeyBind.SelectedItem = _settings.FastForwardPlaybackKey;
            rewindPlaybackKeyBind.SelectedItem = _settings.RewindPlaybackKey;
            volumeUpKeyBind.SelectedItem = _settings.VolumeUpKey;
            volumeDownKeyBind.SelectedItem = _settings.VolumeDownKey;
            toggleMuteKeyBind.SelectedItem = _settings.ToggleMuteKey;
            toggleDeEmphasisKeyBind.SelectedItem = _settings.ToggleDeEmphasisKey;
        }

        /// <summary>
        /// Save back the disc enum values
        /// </summary>
        private void SaveDiscValues()
        {
            ComboBox dataPlayback = this.FindControl<ComboBox>("DataPlayback");
            ComboBox sessionHandling = this.FindControl<ComboBox>("SessionHandling");

            _settings.DataPlayback = (DataPlayback)dataPlayback.SelectedItem;
            _settings.SessionHandling = (SessionHandling)sessionHandling.SelectedItem;
        }

        /// <summary>
        /// Save back all values from keyboard bindings
        /// </summary>
        private void SaveKeyboardList()
        {
            // Access all of the combo boxes
            ComboBox loadImageKeyBind = this.FindControl<ComboBox>("LoadImageKeyBind");
            ComboBox togglePlaybackKeyBind = this.FindControl<ComboBox>("TogglePlaybackKeyBind");
            ComboBox stopPlaybackKeyBind = this.FindControl<ComboBox>("StopPlaybackKeyBind");
            ComboBox ejectKeyBind = this.FindControl<ComboBox>("EjectKeyBind");
            ComboBox nextTrackKeyBind = this.FindControl<ComboBox>("NextTrackKeyBind");
            ComboBox previousTrackKeyBind = this.FindControl<ComboBox>("PreviousTrackKeyBind");
            ComboBox nextIndexKeyBind = this.FindControl<ComboBox>("NextIndexKeyBind");
            ComboBox previousIndexKeyBind = this.FindControl<ComboBox>("PreviousIndexKeyBind");
            ComboBox fastForwardPlaybackKeyBind = this.FindControl<ComboBox>("FastForwardPlaybackKeyBind");
            ComboBox rewindPlaybackKeyBind = this.FindControl<ComboBox>("RewindPlaybackKeyBind");
            ComboBox volumeUpKeyBind = this.FindControl<ComboBox>("VolumeUpKeyBind");
            ComboBox volumeDownKeyBind = this.FindControl<ComboBox>("VolumeDownKeyBind");
            ComboBox toggleMuteKeyBind = this.FindControl<ComboBox>("ToggleMuteKeyBind");
            ComboBox toggleDeEmphasisKeyBind = this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind");

            // Set all of the currently selected items
            _settings.LoadImageKey = (Key)loadImageKeyBind.SelectedItem;
            _settings.TogglePlaybackKey = (Key)togglePlaybackKeyBind.SelectedItem;
            _settings.StopPlaybackKey = (Key)stopPlaybackKeyBind.SelectedItem;
            _settings.EjectKey = (Key)ejectKeyBind.SelectedItem;
            _settings.NextTrackKey = (Key)nextTrackKeyBind.SelectedItem;
            _settings.PreviousTrackKey = (Key)previousTrackKeyBind.SelectedItem;
            _settings.NextIndexKey = (Key)nextIndexKeyBind.SelectedItem;
            _settings.PreviousIndexKey = (Key)previousIndexKeyBind.SelectedItem;
            _settings.FastForwardPlaybackKey = (Key)fastForwardPlaybackKeyBind.SelectedItem;
            _settings.RewindPlaybackKey = (Key)rewindPlaybackKeyBind.SelectedItem;
            _settings.VolumeUpKey = (Key)volumeUpKeyBind.SelectedItem;
            _settings.VolumeDownKey = (Key)volumeDownKeyBind.SelectedItem;
            _settings.ToggleMuteKey = (Key)toggleMuteKeyBind.SelectedItem;
            _settings.ToggleDeEmphasisKey = (Key)toggleDeEmphasisKeyBind.SelectedItem;
        }

        /// <summary>
        /// Generate a list of keyboard keys for mapping
        /// </summary>
        /// <returns></returns>
        private Array GenerateKeyboardList()
        {
           return Enum.GetValues(typeof(Key));
        }
    }
}