using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI
{
    public class PlayerView : UserControl
    {
        /// <summary>
        /// Read-only access to the view model
        /// </summary>
        public PlayerViewModel PlayerViewModel => DataContext as PlayerViewModel;

        /// <summary>
        /// Set of images representing the digits for the UI
        /// </summary>
        private Image[] _digits;

        /// <summary>
        /// Initialize the UI based on the default theme
        /// </summary>
        public PlayerView() : this(null, null) { }

        /// <summary>
        /// Initialize the UI based on the default theme with an existing view model
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        /// <param name="playerViewModel">Existing PlayerViewModel to load in instead of creating a new one</param>
        public PlayerView(PlayerViewModel playerViewModel) : this(null, playerViewModel) { }

        /// <summary>
        /// Initialize the UI based on the currently selected theme
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        /// <param name="playerViewModel">Existing PlayerViewModel to load in instead of creating a new one</param>
        public PlayerView(string xaml, PlayerViewModel playerViewModel)
        {
            if(playerViewModel != null)
                DataContext = playerViewModel;
            else
                DataContext = new PlayerViewModel();

            PlayerViewModel.PropertyChanged += PlayerViewModelStateChanged;

            LoadTheme(xaml);
            InitializeDigits();
        }

        #region Helpers

        /// <summary>
        /// Load an image from the path
        /// </summary>
        /// <param name="path">Path to the image to load</param>
        public async Task<bool> LoadImage(string path)
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PlayerViewModel.Init(path, App.Settings.GenerateMissingTOC, App.Settings.PlayHiddenTracks, App.Settings.PlayDataTracks, App.Settings.AutoPlay, App.Settings.Volume);
                if (PlayerViewModel.Initialized)
                    MainWindow.Instance.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();

                return PlayerViewModel.Initialized;
            });
        }

        /// <summary>
        /// Update the view model with new settings
        /// </summary>
        public void UpdateViewModel()
        {
            PlayerViewModel.SetLoadDataTracks(App.Settings.PlayDataTracks);
            PlayerViewModel.SetLoadHiddenTracks(App.Settings.PlayHiddenTracks);
        }

        /// <summary>
        /// Load the png image for a given character based on the theme
        /// </summary>
        /// <param name="character">Character to load the image for</param>
        /// <returns>Bitmap representing the loaded image</returns>
        private Bitmap GetBitmap(char character)
        {
            try
            {
                if(App.Settings.SelectedTheme == "default")
                {
                    IAssetLoader assets = AvaloniaLocator.Current.GetService<IAssetLoader>();

                    return new Bitmap(assets.Open(new Uri($"avares://RedBookPlayer/Assets/{character}.png")));
                }
                else
                {
                    string themeDirectory = $"{Directory.GetCurrentDirectory()}/themes/{App.Settings.SelectedTheme}";
                    using FileStream stream = File.Open($"{themeDirectory}/{character}.png", FileMode.Open);
                    return new Bitmap(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generate a path selection dialog box
        /// </summary>
        /// <returns>User-selected path, if possible</returns>
        private async Task<string> GetPath()
        {
            var dialog = new OpenFileDialog { AllowMultiple = false };
            List<string> knownExtensions = new Aaru.DiscImages.AaruFormat().KnownExtensions.ToList();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                Extensions = knownExtensions.ConvertAll(e => e.TrimStart('.'))
            });

            return (await dialog.ShowAsync((Window)Parent.Parent))?.FirstOrDefault();
        }

        /// <summary>
        /// Initialize the displayed digits array
        /// </summary>
        private void InitializeDigits()
        {
            _digits = new Image[]
            {
                this.FindControl<Image>("TrackDigit1"),
                this.FindControl<Image>("TrackDigit2"),

                this.FindControl<Image>("IndexDigit1"),
                this.FindControl<Image>("IndexDigit2"),

                this.FindControl<Image>("TimeDigit1"),
                this.FindControl<Image>("TimeDigit2"),
                this.FindControl<Image>("TimeDigit3"),
                this.FindControl<Image>("TimeDigit4"),
                this.FindControl<Image>("TimeDigit5"),
                this.FindControl<Image>("TimeDigit6"),

                this.FindControl<Image>("TotalTracksDigit1"),
                this.FindControl<Image>("TotalTracksDigit2"),

                this.FindControl<Image>("TotalIndexesDigit1"),
                this.FindControl<Image>("TotalIndexesDigit2"),

                this.FindControl<Image>("TotalTimeDigit1"),
                this.FindControl<Image>("TotalTimeDigit2"),
                this.FindControl<Image>("TotalTimeDigit3"),
                this.FindControl<Image>("TotalTimeDigit4"),
                this.FindControl<Image>("TotalTimeDigit5"),
                this.FindControl<Image>("TotalTimeDigit6"),
            };
        }

        /// <summary>
        /// Load the theme from a XAML, if possible
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void LoadTheme(string xaml)
        {
            try
            {
                if(xaml != null)
                    new AvaloniaXamlLoader().Load(xaml, null, this);
                else
                    AvaloniaXamlLoader.Load(this);
            }
            catch
            {
                AvaloniaXamlLoader.Load(this);
            }
        }

        /// <summary>
        /// Update the UI from the view-model
        /// </summary>
        private void PlayerViewModelStateChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                string digitString = PlayerViewModel?.GenerateDigitString() ?? string.Empty.PadLeft(20, '-');
                for(int i = 0; i < _digits.Length; i++)
                {
                    Bitmap digitImage = GetBitmap(digitString[i]);
                    if(_digits[i] != null && digitImage != null)
                        _digits[i].Source = digitImage;
                }
            });
        }

        #endregion

        #region Event Handlers

        public async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            string path = await GetPath();
            if (path == null)
                return;

            await LoadImage(path);
        }

        #endregion
    }
}