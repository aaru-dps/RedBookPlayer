using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer
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

            _settings.Save();
        }

        public void UpdateView() => this.FindControl<TextBlock>("VolumeLabel").Text = _settings.Volume.ToString();

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _themeList                  =  this.FindControl<ListBox>("ThemeList");
            _themeList.SelectionChanged += ThemeList_SelectionChanged;

            List<string> items = new List<string>();
            items.Add("default");

            if(Directory.Exists("themes/"))
            {
                foreach(string dir in Directory.EnumerateDirectories("themes/"))
                {
                    string themeName = dir.Split('/')[1];

                    if (!File.Exists($"themes/{themeName}/view.xaml"))
                        continue;

                    items.Add(themeName);
                }
            }

            _themeList.Items = items;

            this.FindControl<Button>("ApplyButton").Click            += ApplySettings;
            this.FindControl<Slider>("VolumeSlider").PropertyChanged += (s, e) => UpdateView();
        }
    }
}