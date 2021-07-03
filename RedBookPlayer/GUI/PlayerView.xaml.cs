using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

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
        /// <remarks>
        /// TODO: Does it make sense to have this as an array?
        /// </remarks>
        private Image[] _digits;

        /// <summary>
        /// Timer for performing UI updates
        /// </summary>
        private Timer _updateTimer;

        /// <summary>
        /// Last volume for mute toggling
        /// </summary>
        private int? _lastVolume = null;

        public PlayerView() => InitializeComponent(null);

        public PlayerView(string xaml) => InitializeComponent(xaml);

        #region Helpers

        /// <summary>
        /// Generate a path selection dialog box
        /// </summary>
        /// <returns>User-selected path, if possible</returns>
        public async Task<string> GetPath()
        {
            var dialog = new OpenFileDialog { AllowMultiple = false };
            List<string> knownExtensions = new Aaru.DiscImages.AaruFormat().KnownExtensions.ToList();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name       = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                Extensions = knownExtensions.ConvertAll(e => e.TrimStart('.'))
            });

            return (await dialog.ShowAsync((Window)Parent.Parent))?.FirstOrDefault();
        }

        /// <summary>
        /// Load an image from the path
        /// </summary>
        /// <param name="path">Path to the image to load</param>
        public async Task<bool> LoadImage(string path)
        {
            // If the player is currently running, stop it
            if(PlayerViewModel.Playing != true) PlayerViewModel.Playing = null;

            bool result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                PlayerViewModel.Init(path, App.Settings.AutoPlay, App.Settings.Volume);
                return PlayerViewModel.Initialized;
            });

            if(result)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainWindow.Instance.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();
                });
            }

            return result;
        }

        /// <summary>
        /// Load the png image for a given character based on the theme
        /// </summary>
        /// <param name="character">Character to load the image for</param>
        /// <returns>Bitmap representing the loaded image</returns>
        /// <remarks>
        /// TODO: Currently assumes that an image must always exist
        /// </remarks>
        private Bitmap GetBitmap(char character)
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

        /// <summary>
        /// Initialize the UI based on the currently selected theme
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void InitializeComponent(string xaml)
        {
            DataContext = new PlayerViewModel();
            PlayerViewModel.PropertyChanged += UpdateModel;

            // Load the theme
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

            InitializeDigits();

            _updateTimer = new Timer(1000 / 60);

            _updateTimer.Elapsed += (sender, e) =>
            {
                try
                {
                    UpdateView(sender, e);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            };

            _updateTimer.AutoReset = true;
            _updateTimer.Start();
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
        /// Update the Player with the most recent information from the UI
        /// </summary>
        private void UpdateModel(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                PlayerViewModel.UpdateModel();
            });
        }

        /// <summary>
        /// Update the UI with the most recent information from the Player
        /// </summary>
        private void UpdateView(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                string digitString = PlayerViewModel.GenerateDigitString();
                for (int i = 0; i < _digits.Length; i++)
                {
                    if (_digits[i] != null)
                        _digits[i].Source = GetBitmap(digitString[i]);
                }

                PlayerViewModel?.UpdateView();
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

        public void PlayButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Playing = true;

        public void PauseButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Playing = false;

        public void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if(PlayerViewModel.Playing == true)
                PlayerViewModel.Playing = false;
            else
                PlayerViewModel.Playing = true;
        }

        public void StopButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Playing = null;

        public void NextTrackButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.NextTrack();

        public void PreviousTrackButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.PreviousTrack();

        public void NextIndexButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.NextIndex(App.Settings.IndexButtonChangeTrack);

        public void PreviousIndexButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.PreviousIndex(App.Settings.IndexButtonChangeTrack);

        public void FastForwardButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.FastForward();

        public void RewindButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Rewind();

        public void VolumeUpButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Volume++;

        public void VolumeDownButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.Volume--;

        public void MuteToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastVolume == null)
            {
                _lastVolume = PlayerViewModel.Volume;
                PlayerViewModel.Volume = 0;
            }
            else
            {
                PlayerViewModel.Volume = _lastVolume.Value;
                _lastVolume = null;
            }
        }

        public void EnableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.ApplyDeEmphasis = true;

        public void DisableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.ApplyDeEmphasis = false;

        public void EnableDisableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => PlayerViewModel.ApplyDeEmphasis = !PlayerViewModel.ApplyDeEmphasis;

        #endregion
    }
}