using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RedBookPlayer
{
    public class ThemeSelectionWindow : Window
    {
        ListBox themeList;
        string selectedTheme;

        public ThemeSelectionWindow()
        {
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

        public void ApplyTheme(object sender, RoutedEventArgs e)
        {
            if (selectedTheme == "" || selectedTheme == null)
            {
                return;
            }

            if (selectedTheme == "default")
            {
                MainWindow.Instance.ContentControl.Content = new PlayerView();
            }
            else
            {
                string themeDirectory = Directory.GetCurrentDirectory() + "/themes/" + selectedTheme;
                string xamlPath = themeDirectory + "/view.xaml";

                try
                {
                    MainWindow.Instance.ContentControl.Content = new PlayerView(
                        File.ReadAllText(xamlPath).Replace("Source=\"", $"Source=\"file://{themeDirectory}/")
                    );
                }
                catch (System.Xml.XmlException ex)
                {
                    Console.WriteLine($"Error: invalid theme XAML ({ex.Message}), reverting to default");
                    MainWindow.Instance.ContentControl.Content = new PlayerView();
                }
            }

            MainWindow.Instance.Width = ((PlayerView)MainWindow.Instance.ContentControl.Content).Width;
            MainWindow.Instance.Height = ((PlayerView)MainWindow.Instance.ContentControl.Content).Height;

            App.CurrentTheme = selectedTheme;
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

            this.FindControl<Button>("ApplyButton").Click += ApplyTheme;
        }
    }
}