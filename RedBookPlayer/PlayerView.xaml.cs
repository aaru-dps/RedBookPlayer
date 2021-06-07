using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Aaru.DiscImages;
using Aaru.Filters;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace RedBookPlayer
{
    public class PlayerView : UserControl
    {
        /// <summary>
        /// Player representing the internal state and loaded image
        /// </summary>
        public static Player Player = new Player();

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
            List<string> knownExtensions = new AaruFormat().KnownExtensions.ToList();
            dialog.Filters.Add(new FileDialogFilter()
            {
                Name       = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                Extensions = knownExtensions.ConvertAll(e => e.TrimStart('.'))
            });

            return (await dialog.ShowAsync((Window)Parent.Parent))?.FirstOrDefault();
        }

        /// <summary>
        /// Generate the digit string to be interpreted by the UI
        /// </summary>
        /// <returns>String representing the digits for the player</returns>
        private string GenerateDigitString()
        {
            // If the player isn't initialized, return all '-' characters
            if (!Player.Initialized)
                return string.Empty.PadLeft(20, '-');

            // Otherwise, take the current time into account
            ulong sectorTime = Player.CurrentSector;
            if (Player.SectionStartSector != 0)
                sectorTime -= Player.SectionStartSector;
            else
                sectorTime += Player.TimeOffset;

            int[] numbers = new int[]
            {
                Player.CurrentTrack + 1,
                Player.CurrentIndex,

                (int)(sectorTime / (75 * 60)),
                (int)(sectorTime / 75 % 60),
                (int)(sectorTime % 75),

                Player.TotalTracks,
                Player.TotalIndexes,

                (int)(Player.TotalTime / (75 * 60)),
                (int)(Player.TotalTime / 75 % 60),
                (int)(Player.TotalTime % 75),
            };

            return string.Join("", numbers.Select(i => i.ToString().PadLeft(2, '0').Substring(0, 2)));
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
        /// Initialize the displayed digits array
        /// </summary>
        private void Initialize()
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
        /// Initialize the UI based on the currently selected theme
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void InitializeComponent(string xaml)
        {
            DataContext = new PlayerViewModel();

            if (xaml != null)
                new AvaloniaXamlLoader().Load(xaml, null, this);
            else
                AvaloniaXamlLoader.Load(this);

            Initialize();

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
        /// Indicates if the image is considered "playable" or not
        /// </summary>
        /// <param name="image">Aaruformat image file</param>
        /// <returns>True if the image is playble, false otherwise</returns>
        private bool IsPlayableImage(AaruFormat image)
        {
            // Invalid images can't be played
            if (image == null)
                return false;

            // Tape images are not supported
            if (image.IsTape)
                return false;

            // Determine based on media type
            // TODO: Can we be more granular with sub types?
            (string type, string _) = Aaru.CommonTypes.Metadata.MediaType.MediaTypeToString(image.Info.MediaType);
            return type switch
            {
                "Compact Disc" => true,
                "GD" => true, // Requires TOC generation
                _ => false,
            };
        }

        /// <summary>
        /// Update the UI with the most recent information from the Player
        /// </summary>
        private void UpdateView(object sender, ElapsedEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                string digitString = GenerateDigitString();
                for (int i = 0; i < _digits.Length; i++)
                {
                    if (_digits[i] != null)
                        _digits[i].Source = GetBitmap(digitString[i]);
                }

                if (Player.Initialized)
                {
                    PlayerViewModel dataContext = (PlayerViewModel)DataContext;
                    dataContext.HiddenTrack = Player.TimeOffset > 150;
                    dataContext.ApplyDeEmphasis = Player.ApplyDeEmphasis;
                    dataContext.TrackHasEmphasis = Player.TrackHasEmphasis;
                    dataContext.CopyAllowed = Player.CopyAllowed;
                    dataContext.IsAudioTrack = Player.TrackType == TrackTypeValue.Audio;
                    dataContext.IsDataTrack = Player.TrackType == TrackTypeValue.Data;
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

            bool result = await Task.Run(() =>
            {
                var image = new AaruFormat();
                var filter = new ZZZNoFilter();
                filter.Open(path);
                image.Open(filter);

                if (IsPlayableImage(image))
                {
                    Player.Init(image, App.Settings.AutoPlay);
                    return true;
                }
                else
                    return false;
            });

            if (result)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    MainWindow.Instance.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();
                });
            }
        }

        public void PlayButton_Click(object sender, RoutedEventArgs e) => Player.Play();

        public void PauseButton_Click(object sender, RoutedEventArgs e) => Player.Pause();

        public void StopButton_Click(object sender, RoutedEventArgs e) => Player.Stop();

        public void NextTrackButton_Click(object sender, RoutedEventArgs e) => Player.NextTrack();

        public void PreviousTrackButton_Click(object sender, RoutedEventArgs e) => Player.PreviousTrack();

        public void NextIndexButton_Click(object sender, RoutedEventArgs e) => Player.NextIndex(App.Settings.IndexButtonChangeTrack);

        public void PreviousIndexButton_Click(object sender, RoutedEventArgs e) => Player.PreviousIndex(App.Settings.IndexButtonChangeTrack);

        public void FastForwardButton_Click(object sender, RoutedEventArgs e) => Player.FastForward();

        public void RewindButton_Click(object sender, RoutedEventArgs e) => Player.Rewind();

        public void EnableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => Player.ToggleDeEmphasis(true);

        public void DisableDeEmphasisButton_Click(object sender, RoutedEventArgs e) => Player.ToggleDeEmphasis(false);

        #endregion
    }
}