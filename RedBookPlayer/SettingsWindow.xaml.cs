using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer
{
    public class SettingsWindow : Window
    {
        readonly Settings settings;
        string            selectedTheme;
        ListBox           themeList;

        public SettingsWindow() {}

        public SettingsWindow(Settings settings)
        {
            DataContext = this.settings = settings;
            InitializeComponent();
        }

        public void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 0)
            {
                return;
            }

            selectedTheme = (string)e.AddedItems[0];
        }

        public void ApplySettings(object sender, RoutedEventArgs e)
        {
            if((selectedTheme ?? "") != "")
            {
                settings.SelectedTheme = selectedTheme;
                MainWindow.ApplyTheme(selectedTheme);
            }

            PlayerView.Player.Volume = settings.Volume;

            settings.Save();
        }

        public void UpdateView() => this.FindControl<TextBlock>("VolumeLabel").Text = settings.Volume.ToString();

        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            themeList                  =  this.FindControl<ListBox>("ThemeList");
            themeList.SelectionChanged += ThemeList_SelectionChanged;

            List<string> items = new List<string>();
            items.Add("default");

            if(Directory.Exists("themes/"))
            {
                foreach(string dir in Directory.EnumerateDirectories("themes/"))
                {
                    string themeName = dir.Split('/')[1];

                    if(!File.Exists($"themes/{themeName}/view.xaml"))
                    {
                        continue;
                    }

                    items.Add(themeName);
                }
            }

            themeList.Items = items;

            this.FindControl<Button>("ApplyButton").Click            += ApplySettings;
            this.FindControl<Slider>("VolumeSlider").PropertyChanged += (s, e) => UpdateView();
        }
    }
}