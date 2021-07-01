using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer.GUI
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
                MainWindow.ApplyTheme(_selectedTheme);
            }

            SaveKeyboardList();
            _settings.Save();
        }

        public void UpdateView() => this.FindControl<TextBlock>("VolumeLabel").Text = _settings.Volume.ToString();

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            PopulateThemes();
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
        /// Populate all of the keyboard bindings
        /// </summary>
        private void PopulateKeyboardList()
        {
            // Access all of the combo boxes
            ComboBox LoadImageKeyBind = this.FindControl<ComboBox>("LoadImageKeyBind");
            ComboBox TogglePlaybackKeyBind = this.FindControl<ComboBox>("TogglePlaybackKeyBind");
            ComboBox StopPlaybackKeyBind = this.FindControl<ComboBox>("StopPlaybackKeyBind");
            ComboBox NextTrackKeyBind = this.FindControl<ComboBox>("NextTrackKeyBind");
            ComboBox PreviousTrackKeyBind = this.FindControl<ComboBox>("PreviousTrackKeyBind");
            ComboBox NextIndexKeyBind = this.FindControl<ComboBox>("NextIndexKeyBind");
            ComboBox PreviousIndexKeyBind = this.FindControl<ComboBox>("PreviousIndexKeyBind");
            ComboBox FastForwardPlaybackKeyBind = this.FindControl<ComboBox>("FastForwardPlaybackKeyBind");
            ComboBox RewindPlaybackKeyBind = this.FindControl<ComboBox>("RewindPlaybackKeyBind");
            ComboBox ToggleDeEmphasisKeyBind = this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind");

            // Assign the list of values to all of them
            Array keyboardList = GenerateKeyboardList();
            LoadImageKeyBind.Items = keyboardList;
            TogglePlaybackKeyBind.Items = keyboardList;
            StopPlaybackKeyBind.Items = keyboardList;
            NextTrackKeyBind.Items = keyboardList;
            PreviousTrackKeyBind.Items = keyboardList;
            NextIndexKeyBind.Items = keyboardList;
            PreviousIndexKeyBind.Items = keyboardList;
            FastForwardPlaybackKeyBind.Items = keyboardList;
            RewindPlaybackKeyBind.Items = keyboardList;
            ToggleDeEmphasisKeyBind.Items = keyboardList;

            // Set all of the currently selected items
            LoadImageKeyBind.SelectedItem = _settings.LoadImageKey;
            TogglePlaybackKeyBind.SelectedItem = _settings.TogglePlaybackKey;
            StopPlaybackKeyBind.SelectedItem = _settings.StopPlaybackKey;
            NextTrackKeyBind.SelectedItem = _settings.NextTrackKey;
            PreviousTrackKeyBind.SelectedItem = _settings.PreviousTrackKey;
            NextIndexKeyBind.SelectedItem = _settings.NextIndexKey;
            PreviousIndexKeyBind.SelectedItem = _settings.PreviousIndexKey;
            FastForwardPlaybackKeyBind.SelectedItem = _settings.FastForwardPlaybackKey;
            RewindPlaybackKeyBind.SelectedItem = _settings.RewindPlaybackKey;
            ToggleDeEmphasisKeyBind.SelectedItem = _settings.ToggleDeEmphasisKey;
        }

        /// <summary>
        /// Save back all values from keyboard bindings
        /// </summary>
        private void SaveKeyboardList()
        {
            // Access all of the combo boxes
            ComboBox LoadImageKeyBind = this.FindControl<ComboBox>("LoadImageKeyBind");
            ComboBox TogglePlaybackKeyBind = this.FindControl<ComboBox>("TogglePlaybackKeyBind");
            ComboBox StopPlaybackKeyBind = this.FindControl<ComboBox>("StopPlaybackKeyBind");
            ComboBox NextTrackKeyBind = this.FindControl<ComboBox>("NextTrackKeyBind");
            ComboBox PreviousTrackKeyBind = this.FindControl<ComboBox>("PreviousTrackKeyBind");
            ComboBox NextIndexKeyBind = this.FindControl<ComboBox>("NextIndexKeyBind");
            ComboBox PreviousIndexKeyBind = this.FindControl<ComboBox>("PreviousIndexKeyBind");
            ComboBox FastForwardPlaybackKeyBind = this.FindControl<ComboBox>("FastForwardPlaybackKeyBind");
            ComboBox RewindPlaybackKeyBind = this.FindControl<ComboBox>("RewindPlaybackKeyBind");
            ComboBox ToggleDeEmphasisKeyBind = this.FindControl<ComboBox>("ToggleDeEmphasisKeyBind");

            // Set all of the currently selected items
            _settings.LoadImageKey = (Key)LoadImageKeyBind.SelectedItem;
            _settings.TogglePlaybackKey = (Key)TogglePlaybackKeyBind.SelectedItem;
            _settings.StopPlaybackKey = (Key)StopPlaybackKeyBind.SelectedItem;
            _settings.NextTrackKey = (Key)NextTrackKeyBind.SelectedItem;
            _settings.PreviousTrackKey = (Key)PreviousTrackKeyBind.SelectedItem;
            _settings.NextIndexKey = (Key)NextIndexKeyBind.SelectedItem;
            _settings.PreviousIndexKey = (Key)PreviousIndexKeyBind.SelectedItem;
            _settings.FastForwardPlaybackKey = (Key)FastForwardPlaybackKeyBind.SelectedItem;
            _settings.RewindPlaybackKey = (Key)RewindPlaybackKeyBind.SelectedItem;
            _settings.ToggleDeEmphasisKey = (Key)ToggleDeEmphasisKeyBind.SelectedItem;
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