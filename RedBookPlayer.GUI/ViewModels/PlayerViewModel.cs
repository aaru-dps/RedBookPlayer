using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ReactiveUI;
using RedBookPlayer.GUI.Views;
using RedBookPlayer.Models;
using RedBookPlayer.Models.Discs;
using RedBookPlayer.Models.Hardware;

namespace RedBookPlayer.GUI.ViewModels
{
    public class PlayerViewModel : ReactiveObject
    {
        /// <summary>
        /// Player representing the internal state
        /// </summary>
        private readonly Player _player;

        /// <summary>
        /// Set of images representing the digits for the UI
        /// </summary>
        private Image[] _digits;

        #region Player Passthrough

        #region OpticalDisc Passthrough

        /// <summary>
        /// Current track number
        /// </summary>
        public int CurrentTrackNumber
        {
            get => _currentTrackNumber;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackNumber, value);
        }

        /// <summary>
        /// Current track index
        /// </summary>
        public ushort CurrentTrackIndex
        {
            get => _currentTrackIndex;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackIndex, value);
        }

        /// <summary>
        /// Current sector number
        /// </summary>
        public ulong CurrentSector
        {
            get => _currentSector;
            private set => this.RaiseAndSetIfChanged(ref _currentSector, value);
        }

        /// <summary>
        /// Represents the sector starting the section
        /// </summary>
        public ulong SectionStartSector
        {
            get => _sectionStartSector;
            private set => this.RaiseAndSetIfChanged(ref _sectionStartSector, value);
        }

        /// <summary>
        /// Represents if the disc has a hidden track
        /// </summary>
        public bool HiddenTrack
        {
            get => _hasHiddenTrack;
            private set => this.RaiseAndSetIfChanged(ref _hasHiddenTrack, value);
        }

        /// <summary>
        /// Represents the 4CH flag [CompactDisc only]
        /// </summary>
        public bool QuadChannel
        {
            get => _quadChannel;
            private set => this.RaiseAndSetIfChanged(ref _quadChannel, value);
        }

        /// <summary>
        /// Represents the DATA flag [CompactDisc only]
        /// </summary>
        public bool IsDataTrack
        {
            get => _isDataTrack;
            private set => this.RaiseAndSetIfChanged(ref _isDataTrack, value);
        }

        /// <summary>
        /// Represents the DCP flag [CompactDisc only]
        /// </summary>
        public bool CopyAllowed
        {
            get => _copyAllowed;
            private set => this.RaiseAndSetIfChanged(ref _copyAllowed, value);
        }

        /// <summary>
        /// Represents the PRE flag [CompactDisc only]
        /// </summary>
        public bool TrackHasEmphasis
        {
            get => _trackHasEmphasis;
            private set => this.RaiseAndSetIfChanged(ref _trackHasEmphasis, value);
        }

        /// <summary>
        /// Represents the total tracks on the disc
        /// </summary>
        public int TotalTracks => _player.TotalTracks;

        /// <summary>
        /// Represents the total indices on the disc
        /// </summary>
        public int TotalIndexes => _player.TotalIndexes;

        /// <summary>
        /// Total sectors in the image
        /// </summary>
        public ulong TotalSectors => _player.TotalSectors;

        /// <summary>
        /// Represents the time adjustment offset for the disc
        /// </summary>
        public ulong TimeOffset => _player.TimeOffset;

        /// <summary>
        /// Represents the total playing time for the disc
        /// </summary>
        public ulong TotalTime => _player.TotalTime;

        private int _currentTrackNumber;
        private ushort _currentTrackIndex;
        private ulong _currentSector;
        private ulong _sectionStartSector;

        private bool _hasHiddenTrack;
        private bool _quadChannel;
        private bool _isDataTrack;
        private bool _copyAllowed;
        private bool _trackHasEmphasis;

        #endregion

        #region SoundOutput Passthrough

        /// <summary>
        /// Indicate if the model is ready to be used
        /// </summary>
        public bool Initialized
        {
            get => _initialized;
            private set => this.RaiseAndSetIfChanged(ref _initialized, value);
        }

        /// <summary>
        /// Indicate if the output is playing
        /// </summary>
        public PlayerState PlayerState
        {
            get => _playerState;
            private set => this.RaiseAndSetIfChanged(ref _playerState, value);
        }

        /// <summary>
        /// Indicates how to handle playback of data tracks
        /// </summary>
        public DataPlayback DataPlayback
        {
            get => _dataPlayback;
            private set => this.RaiseAndSetIfChanged(ref _dataPlayback, value);
        }

        /// <summary>
        /// Indicates the repeat mode
        /// </summary>
        public RepeatMode RepeatMode
        {
            get => _repeatMode;
            private set => this.RaiseAndSetIfChanged(ref _repeatMode, value);
        }

        /// <summary>
        /// Indicates if de-emphasis should be applied
        /// </summary>
        public bool ApplyDeEmphasis
        {
            get => _applyDeEmphasis;
            private set => this.RaiseAndSetIfChanged(ref _applyDeEmphasis, value);
        }

        /// <summary>
        /// Current playback volume
        /// </summary>
        public int Volume
        {
            get => _volume;
            private set => this.RaiseAndSetIfChanged(ref _volume, value);
        }

        private bool _initialized;
        private PlayerState _playerState;
        private DataPlayback _dataPlayback;
        private RepeatMode _repeatMode;
        private bool _applyDeEmphasis;
        private int _volume;

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command for handling keypresses
        /// </summary>
        public ReactiveCommand<KeyEventArgs, Unit> KeyPressCommand { get; }

        /// <summary>
        /// Command for loading a disc
        /// </summary>
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }

        /// <summary>
        /// Command for loading a disc from drag and drop
        /// </summary>
        public ReactiveCommand<DragEventArgs, Unit> LoadDragDropCommand { get; }

        #region Playback

        /// <summary>
        /// Command for beginning playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> PlayCommand { get; }

        /// <summary>
        /// Command for pausing playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> PauseCommand { get; }

        /// <summary>
        /// Command for pausing playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> TogglePlayPauseCommand { get; }

        /// <summary>
        /// Command for stopping playback
        /// </summary>
        public ReactiveCommand<Unit, Unit> StopCommand { get; }

        /// <summary>
        /// Command for ejecting the current disc
        /// </summary>
        public ReactiveCommand<Unit, Unit> EjectCommand { get; }

        /// <summary>
        /// Command for moving to the next track
        /// </summary>
        public ReactiveCommand<Unit, Unit> NextTrackCommand { get; }

        /// <summary>
        /// Command for moving to the previous track
        /// </summary>
        public ReactiveCommand<Unit, Unit> PreviousTrackCommand { get; }

        /// <summary>
        /// Command for moving to the next index
        /// </summary>
        public ReactiveCommand<Unit, Unit> NextIndexCommand { get; }

        /// <summary>
        /// Command for moving to the previous index
        /// </summary>
        public ReactiveCommand<Unit, Unit> PreviousIndexCommand { get; }

        /// <summary>
        /// Command for fast forwarding
        /// </summary>
        public ReactiveCommand<Unit, Unit> FastForwardCommand { get; }

        /// <summary>
        /// Command for rewinding
        /// </summary>
        public ReactiveCommand<Unit, Unit> RewindCommand { get; }

        #endregion

        #region Volume

        /// <summary>
        /// Command for incrementing volume
        /// </summary>
        public ReactiveCommand<Unit, Unit> VolumeUpCommand { get; }

        /// <summary>
        /// Command for decrementing volume
        /// </summary>
        public ReactiveCommand<Unit, Unit> VolumeDownCommand { get; }

        /// <summary>
        /// Command for toggling mute
        /// </summary>
        public ReactiveCommand<Unit, Unit> ToggleMuteCommand { get; }

        #endregion

        #region Emphasis

        /// <summary>
        /// Command for enabling de-emphasis
        /// </summary>
        public ReactiveCommand<Unit, Unit> EnableDeEmphasisCommand { get; }

        /// <summary>
        /// Command for disabling de-emphasis
        /// </summary>
        public ReactiveCommand<Unit, Unit> DisableDeEmphasisCommand { get; }

        /// <summary>
        /// Command for toggling de-emphasis
        /// </summary>
        public ReactiveCommand<Unit, Unit> ToggleDeEmphasisCommand { get; }

        #endregion

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public PlayerViewModel()
        {
            // Initialize commands
            KeyPressCommand = ReactiveCommand.Create<KeyEventArgs>(ExecuteKeyPress);
            LoadCommand = ReactiveCommand.Create(ExecuteLoad);
            LoadDragDropCommand = ReactiveCommand.Create<DragEventArgs>(ExecuteLoadDragDrop);

            PlayCommand = ReactiveCommand.Create(ExecutePlay);
            PauseCommand = ReactiveCommand.Create(ExecutePause);
            TogglePlayPauseCommand = ReactiveCommand.Create(ExecuteTogglePlayPause);
            StopCommand = ReactiveCommand.Create(ExecuteStop);
            EjectCommand = ReactiveCommand.Create(ExecuteEject);
            NextTrackCommand = ReactiveCommand.Create(ExecuteNextTrack);
            PreviousTrackCommand = ReactiveCommand.Create(ExecutePreviousTrack);
            NextIndexCommand = ReactiveCommand.Create(ExecuteNextIndex);
            PreviousIndexCommand = ReactiveCommand.Create(ExecutePreviousIndex);
            FastForwardCommand = ReactiveCommand.Create(ExecuteFastForward);
            RewindCommand = ReactiveCommand.Create(ExecuteRewind);

            VolumeUpCommand = ReactiveCommand.Create(ExecuteVolumeUp);
            VolumeDownCommand = ReactiveCommand.Create(ExecuteVolumeDown);
            ToggleMuteCommand = ReactiveCommand.Create(ExecuteToggleMute);

            EnableDeEmphasisCommand = ReactiveCommand.Create(ExecuteEnableDeEmphasis);
            DisableDeEmphasisCommand = ReactiveCommand.Create(ExecuteDisableDeEmphasis);
            ToggleDeEmphasisCommand = ReactiveCommand.Create(ExecuteToggleDeEmphasis);

            // Initialize Player
            _player = new Player(App.Settings.Volume);
            PlayerState = PlayerState.NoDisc;
        }

        /// <summary>
        /// Initialize the view model with a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="options">Options to pass to the optical disc factory</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, OpticalDiscOptions options, bool autoPlay)
        {
            // Stop current playback, if necessary
            if(PlayerState != PlayerState.NoDisc) ExecuteStop();

            // Attempt to initialize Player
            _player.Init(path, options, autoPlay);
            if(_player.Initialized)
            {
                _player.PropertyChanged += PlayerStateChanged;
                PlayerStateChanged(this, null);
            }
        }

        #region Playback

        /// <summary>
        /// Begin playback
        /// </summary>
        public void ExecutePlay() => _player?.Play();

        /// <summary>
        /// Pause current playback
        /// </summary>
        public void ExecutePause() => _player?.Pause();

        /// <summary>
        /// Toggle playback
        /// </summary>
        public void ExecuteTogglePlayPause() => _player?.TogglePlayback();

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void ExecuteStop() => _player?.Stop();

        /// <summary>
        /// Eject the currently loaded disc
        /// </summary>
        public void ExecuteEject() => _player?.Eject();

        /// <summary>
        /// Move to the next playable track
        /// </summary>
        public void ExecuteNextTrack() => _player?.NextTrack();

        /// <summary>
        /// Move to the previous playable track
        /// </summary>
        public void ExecutePreviousTrack() => _player?.PreviousTrack();

        /// <summary>
        /// Move to the next index
        /// </summary>
        public void ExecuteNextIndex() => _player?.NextIndex(App.Settings.IndexButtonChangeTrack);

        /// <summary>
        /// Move to the previous index
        /// </summary>
        public void ExecutePreviousIndex() => _player?.PreviousIndex(App.Settings.IndexButtonChangeTrack);

        /// <summary>
        /// Fast-forward playback by 75 sectors, if possible
        /// </summary>
        public void ExecuteFastForward() => _player?.FastForward();

        /// <summary>
        /// Rewind playback by 75 sectors, if possible
        /// </summary>
        public void ExecuteRewind() => _player?.Rewind();

        #endregion

        #region Volume

        /// <summary>
        /// Increment the volume value
        /// </summary>
        public void ExecuteVolumeUp() => _player?.VolumeUp();

        /// <summary>
        /// Decrement the volume value
        /// </summary>
        public void ExecuteVolumeDown() => _player?.VolumeDown();

        /// <summary>
        /// Set the value for the volume
        /// </summary>
        /// <param name="volume">New volume value</param>
        public void ExecuteSetVolume(int volume) => _player?.SetVolume(volume);

        /// <summary>
        /// Temporarily mute playback
        /// </summary>
        public void ExecuteToggleMute() => _player?.ToggleMute();

        #endregion

        #region Emphasis

        /// <summary>
        /// Enable de-emphasis
        /// </summary>
        public void ExecuteEnableDeEmphasis() => _player?.EnableDeEmphasis();

        /// <summary>
        /// Disable de-emphasis
        /// </summary>
        public void ExecuteDisableDeEmphasis() => _player?.DisableDeEmphasis();

        /// <summary>
        /// Toggle de-emphasis
        /// </summary>
        public void ExecuteToggleDeEmphasis() => _player?.ToggleDeEmphasis();

        #endregion

        #region Helpers

        /// <summary>
        /// Apply a custom theme to the player
        /// </summary>
        /// <param name="theme">Path to the theme under the themes directory</param>
        public void ApplyTheme(string theme)
        {
            // If the PlayerView isn't set, don't do anything
            if(App.MainWindow.PlayerView == null)
                return;

            // If no theme path is provided, we can ignore
            if(string.IsNullOrWhiteSpace(theme))
                return;

            // If the theme name is "default", we assume the internal theme is used
            if(theme.Equals("default", StringComparison.CurrentCultureIgnoreCase))
            {
                LoadTheme(null);
            }
            else
            {
                string themeDirectory = $"{Directory.GetCurrentDirectory()}/themes/{theme}";
                string xamlPath = $"{themeDirectory}/view.xaml";

                if(!File.Exists(xamlPath))
                {
                    Console.WriteLine("Warning: specified theme doesn't exist, reverting to default");
                    return;
                }

                try
                {
                    string xaml = File.ReadAllText(xamlPath);
                    xaml = xaml.Replace("Source=\"", $"Source=\"file://{themeDirectory}/");
                    LoadTheme(xaml);
                }
                catch(XmlException ex)
                {
                    Console.WriteLine($"Error: invalid theme XAML ({ex.Message}), reverting to default");
                    LoadTheme(null);
                }
            }

            App.MainWindow.Width = App.MainWindow.PlayerView.Width;
            App.MainWindow.Height = App.MainWindow.PlayerView.Height;
            InitializeDigits();
        }

        /// <summary>
        /// Execute the result of a keypress
        /// </summary>
        public void ExecuteKeyPress(KeyEventArgs e)
        {
            // Open settings window
            if(e.Key == App.Settings.OpenSettingsKey)
            {
                App.MainWindow.SettingsWindow = new SettingsWindow() { DataContext = App.Settings };
                App.MainWindow.SettingsWindow.Closed += OnSettingsClosed;
                App.MainWindow.SettingsWindow.Show();
            }

            // Load image
            else if(e.Key == App.Settings.LoadImageKey)
            {
                ExecuteLoad();
            }

            // Toggle playback
            else if(e.Key == App.Settings.TogglePlaybackKey || e.Key == Key.MediaPlayPause)
            {
                ExecuteTogglePlayPause();
            }

            // Stop playback
            else if(e.Key == App.Settings.StopPlaybackKey || e.Key == Key.MediaStop)
            {
                ExecuteStop();
            }

            // Eject
            else if(e.Key == App.Settings.EjectKey)
            {
                ExecuteEject();
            }

            // Next Track
            else if(e.Key == App.Settings.NextTrackKey || e.Key == Key.MediaNextTrack)
            {
                ExecuteNextTrack();
            }

            // Previous Track
            else if(e.Key == App.Settings.PreviousTrackKey || e.Key == Key.MediaPreviousTrack)
            {
                ExecutePreviousTrack();
            }

            // Next Index
            else if(e.Key == App.Settings.NextIndexKey)
            {
                ExecuteNextIndex();
            }

            // Previous Index
            else if(e.Key == App.Settings.PreviousIndexKey)
            {
                ExecutePreviousIndex();
            }

            // Fast Foward
            else if(e.Key == App.Settings.FastForwardPlaybackKey)
            {
                ExecuteFastForward();
            }

            // Rewind
            else if(e.Key == App.Settings.RewindPlaybackKey)
            {
                ExecuteRewind();
            }

            // Volume Up
            else if(e.Key == App.Settings.VolumeUpKey || e.Key == Key.VolumeUp)
            {
                int increment = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    increment *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    increment *= 5;

                ExecuteSetVolume(Volume + increment);
            }

            // Volume Down
            else if(e.Key == App.Settings.VolumeDownKey || e.Key == Key.VolumeDown)
            {
                int decrement = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    decrement *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    decrement *= 5;

                ExecuteSetVolume(Volume - decrement);
            }

            // Mute Toggle
            else if(e.Key == App.Settings.ToggleMuteKey || e.Key == Key.VolumeMute)
            {
                ExecuteToggleMute();
            }

            // Emphasis Toggle
            else if(e.Key == App.Settings.ToggleDeEmphasisKey)
            {
                ExecuteToggleDeEmphasis();
            }
        }

        /// <summary>
        /// Load a disc image from a selection box
        /// </summary>
        public async void ExecuteLoad()
        {
            string path = await GetPath();
            if(path == null)
                return;

            await LoadImage(path);
        }

        /// <summary>
        /// Load the first valid drag-and-dropped disc image
        /// </summary>
        public async void ExecuteLoadDragDrop(DragEventArgs e)
        {
            IEnumerable<string> fileNames = e.Data.GetFileNames();
            foreach(string filename in fileNames)
            {
                bool loaded = await LoadImage(filename);
                if(loaded)
                    break;
            }
        }

        /// <summary>
        /// Initialize the displayed digits array
        /// </summary>
        public void InitializeDigits()
        {
            if(!(App.MainWindow.ContentControl.Content is PlayerView playerView))
                return;

            _digits = new Image[]
            {
                playerView.FindControl<Image>("TrackDigit1"),
                playerView.FindControl<Image>("TrackDigit2"),

                playerView.FindControl<Image>("IndexDigit1"),
                playerView.FindControl<Image>("IndexDigit2"),

                playerView.FindControl<Image>("TimeDigit1"),
                playerView.FindControl<Image>("TimeDigit2"),
                playerView.FindControl<Image>("TimeDigit3"),
                playerView.FindControl<Image>("TimeDigit4"),
                playerView.FindControl<Image>("TimeDigit5"),
                playerView.FindControl<Image>("TimeDigit6"),

                playerView.FindControl<Image>("TotalTracksDigit1"),
                playerView.FindControl<Image>("TotalTracksDigit2"),

                playerView.FindControl<Image>("TotalIndexesDigit1"),
                playerView.FindControl<Image>("TotalIndexesDigit2"),

                playerView.FindControl<Image>("TotalTimeDigit1"),
                playerView.FindControl<Image>("TotalTimeDigit2"),
                playerView.FindControl<Image>("TotalTimeDigit3"),
                playerView.FindControl<Image>("TotalTimeDigit4"),
                playerView.FindControl<Image>("TotalTimeDigit5"),
                playerView.FindControl<Image>("TotalTimeDigit6"),
            };
        }

        /// <summary>
        /// Load an image from the path
        /// </summary>
        /// <param name="path">Path to the image to load</param>
        public async Task<bool> LoadImage(string path)
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                OpticalDiscOptions options = new OpticalDiscOptions
                {
                    DataPlayback = App.Settings.DataPlayback,
                    GenerateMissingToc = App.Settings.GenerateMissingTOC,
                    LoadHiddenTracks = App.Settings.PlayHiddenTracks,
                    SessionHandling = App.Settings.SessionHandling,
                };

                Init(path, options, App.Settings.AutoPlay);
                if(Initialized)
                    App.MainWindow.Title = "RedBookPlayer - " + path.Split('/').Last().Split('\\').Last();

                return Initialized;
            });
        }

        /// <summary>
        /// Refresh the view model from the current settings
        /// </summary>
        public void RefreshFromSettings()
        {
            SetDataPlayback(App.Settings.DataPlayback);
            SetLoadHiddenTracks(App.Settings.PlayHiddenTracks);
            SetRepeatMode(App.Settings.RepeatMode);
            SetSessionHandling(App.Settings.SessionHandling);
        }

        /// <summary>
        /// Set data playback method [CompactDisc only]
        /// </summary>
        /// <param name="dataPlayback">New playback value</param>
        public void SetDataPlayback(DataPlayback dataPlayback) => _player?.SetDataPlayback(dataPlayback);

        /// <summary>
        /// Set the value for loading hidden tracks [CompactDisc only]
        /// </summary>
        /// <param name="load">True to enable loading hidden tracks, false otherwise</param>
        public void SetLoadHiddenTracks(bool load) => _player?.SetLoadHiddenTracks(load);

        /// <summary>
        /// Set repeat mode
        /// </summary>
        /// <param name="repeatMode">New repeat mode value</param>
        public void SetRepeatMode(RepeatMode repeatMode) => _player?.SetRepeatMode(repeatMode);

        /// <summary>
        /// Set session handling
        /// </summary>
        /// <param name="sessionHandling">New session handling value</param>
        public void SetSessionHandling(SessionHandling sessionHandling) => _player?.SetSessionHandling(sessionHandling);

        /// <summary>
        /// Generate the digit string to be interpreted by the frontend
        /// </summary>
        /// <returns>String representing the digits for the frontend</returns>
        private string GenerateDigitString()
        {
            // If the disc isn't initialized, return all '-' characters
            if(Initialized != true)
                return string.Empty.PadLeft(20, '-');

            int usableTrackNumber = CurrentTrackNumber;
            if(usableTrackNumber < 0)
                usableTrackNumber = 0;
            else if(usableTrackNumber > 99)
                usableTrackNumber = 99;

            // Otherwise, take the current time into account
            ulong sectorTime = GetCurrentSectorTime();

            int[] numbers = new int[]
            {
                usableTrackNumber,
                CurrentTrackIndex,

                (int)(sectorTime / (75 * 60)),
                (int)(sectorTime / 75 % 60),
                (int)(sectorTime % 75),

                TotalTracks,
                TotalIndexes,

                (int)(TotalTime / (75 * 60)),
                (int)(TotalTime / 75 % 60),
                (int)(TotalTime % 75),
            };

            return string.Join("", numbers.Select(i => i.ToString().PadLeft(2, '0').Substring(0, 2)));
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
        /// Get current sector time, accounting for offsets
        /// </summary>
        /// <returns>ulong representing the current sector time</returns>
        private ulong GetCurrentSectorTime()
        {
            ulong sectorTime = CurrentSector;
            if(SectionStartSector != 0)
                sectorTime -= SectionStartSector;
            else if(CurrentTrackNumber > 0)
                sectorTime += TimeOffset;

            return sectorTime;
        }

        /// <summary>
        /// Generate a path selection dialog box
        /// </summary>
        /// <returns>User-selected path, if possible</returns>
        private async Task<string> GetPath()
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new OpenFileDialog { AllowMultiple = false };
                List<string> knownExtensions = new Aaru.DiscImages.AaruFormat().KnownExtensions.ToList();
                dialog.Filters.Add(new FileDialogFilter()
                {
                    Name = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                    Extensions = knownExtensions.ConvertAll(e => e.TrimStart('.'))
                });

                return (await dialog.ShowAsync(App.MainWindow))?.FirstOrDefault();
            });
        }

        /// <summary>
        /// Load the theme from a XAML, if possible
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void LoadTheme(string xaml)
        {
            // If the view is null, we can't load the theme
            if(App.MainWindow.PlayerView == null)
                return;

            try
            {
                if(xaml != null)
                    new AvaloniaXamlLoader().Load(xaml, null, App.MainWindow.PlayerView);
                else
                    AvaloniaXamlLoader.Load(App.MainWindow.PlayerView);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            // Reset the data context
            App.MainWindow.PlayerView.DataContext = this;
        }

        /// <summary>
        /// Handle the settings window closing
        /// </summary>
        private void OnSettingsClosed(object sender, EventArgs e) => RefreshFromSettings();

        /// <summary>
        /// Update the view-model from the Player
        /// </summary>
        private void PlayerStateChanged(object sender, PropertyChangedEventArgs e)
        {
            if(_player == null)
                return;

            if(!_player.Initialized)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    App.MainWindow.Title = "RedBookPlayer";
                });
            }

            Initialized = _player.Initialized;

            CurrentTrackNumber = _player.CurrentTrackNumber;
            CurrentTrackIndex = _player.CurrentTrackIndex;
            CurrentSector = _player.CurrentSector;
            SectionStartSector = _player.SectionStartSector;

            HiddenTrack = _player.HiddenTrack;

            QuadChannel = _player.QuadChannel;
            IsDataTrack = _player.IsDataTrack;
            CopyAllowed = _player.CopyAllowed;
            TrackHasEmphasis = _player.TrackHasEmphasis;

            PlayerState = _player.PlayerState;
            DataPlayback = _player.DataPlayback;
            RepeatMode = _player.RepeatMode;
            ApplyDeEmphasis = _player.ApplyDeEmphasis;
            Volume = _player.Volume;

            UpdateDigits();
        }

        /// <summary>
        /// Update UI 
        /// </summary>
        private void UpdateDigits()
        {
            // Ensure the digits
            if(_digits == null)
                InitializeDigits();

            Dispatcher.UIThread.Post(() =>
            {
                string digitString = GenerateDigitString() ?? string.Empty.PadLeft(20, '-');
                for(int i = 0; i < _digits.Length; i++)
                {
                    Bitmap digitImage = GetBitmap(digitString[i]);
                    if(_digits[i] != null && digitImage != null)
                        _digits[i].Source = digitImage;
                }
            });
        }

        #endregion
    }
}