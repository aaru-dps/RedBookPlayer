using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer
{
    public class SettingsWindow : Window
    {
        Settings settings;
        ListBox themeList;
        string selectedTheme;

        public SettingsWindow() { }

        public SettingsWindow(Settings settings)
        {
            this.DataContext = this.settings = settings;
            InitializeComponent();
        }

        public void ThemeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }

            selectedTheme = (string)e.AddedItems[0];
        }

        public void ApplySettings(object sender, RoutedEventArgs e)
        {
            if ((selectedTheme ?? "") != "")
            {
                settings.SelectedTheme = selectedTheme;
                MainWindow.ApplyTheme(selectedTheme);
            }

            settings.Save();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            themeList = this.FindControl<ListBox>("ThemeList");
            themeList.SelectionChanged += ThemeList_SelectionChanged;

            List<String> items = new List<String>();
            items.Add("default");

            if (Directory.Exists("themes/"))
            {
                foreach (string dir in Directory.EnumerateDirectories("themes/"))
                {
                    string themeName = dir.Split('/')[1];

                    if (!File.Exists($"themes/{themeName}/view.xaml"))
                    {
                        continue;
                    }

                    items.Add(themeName);
                }
            }

            themeList.Items = items;

            this.FindControl<Button>("ApplyButton").Click += ApplySettings;
        }
    }
}