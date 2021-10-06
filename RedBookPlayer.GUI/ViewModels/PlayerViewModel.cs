using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ReactiveUI;
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

        /// <summary>
        /// Currently selected disc
        /// </summary>
        public int CurrentDisc
        {
            get => _currentDisc;
            private set => this.RaiseAndSetIfChanged(ref _currentDisc, value);
        }

        private int _currentDisc;

        #region OpticalDisc Passthrough

        /// <summary>
        /// Path to the disc image
        /// </summary>
        public string ImagePath
        {
            get => _imagePath;
            private set => this.RaiseAndSetIfChanged(ref _imagePath, value);
        }

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
        /// Current track session
        /// </summary>
        public ushort CurrentTrackSession
        {
            get => _currentTrackSession;
            private set => this.RaiseAndSetIfChanged(ref _currentTrackSession, value);
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

        private string _imagePath;
        private int _currentTrackNumber;
        private ushort _currentTrackIndex;
        private ushort _currentTrackSession;
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
        /// Command for loading a disc
        /// </summary>
        public ReactiveCommand<Unit, Unit> LoadCommand { get; }

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
        /// Command for moving to the next disc
        /// </summary>
        public ReactiveCommand<Unit, Unit> NextDiscCommand { get; }

        /// <summary>
        /// Command for moving to the previous disc
        /// </summary>
        public ReactiveCommand<Unit, Unit> PreviousDiscCommand { get; }

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
            LoadCommand = ReactiveCommand.Create(ExecuteLoad);

            PlayCommand = ReactiveCommand.Create(ExecutePlay);
            PauseCommand = ReactiveCommand.Create(ExecutePause);
            TogglePlayPauseCommand = ReactiveCommand.Create(ExecuteTogglePlayPause);
            StopCommand = ReactiveCommand.Create(ExecuteStop);
            EjectCommand = ReactiveCommand.Create(ExecuteEject);
            NextDiscCommand = ReactiveCommand.Create(ExecuteNextDisc);
            PreviousDiscCommand = ReactiveCommand.Create(ExecutePreviousDisc);
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
            _player = new Player(App.Settings.NumberOfDiscs, App.Settings.Volume);
            PlayerState = PlayerState.NoDisc;
        }

        /// <summary>
        /// Initialize the view model with a given image path
        /// </summary>
        /// <param name="path">Path to the disc image</param>
        /// <param name="playerOptions">Options to pass to the player</param>
        /// <param name="opticalDiscOptions">Options to pass to the optical disc factory</param>
        /// <param name="autoPlay">True if playback should begin immediately, false otherwise</param>
        public void Init(string path, PlayerOptions playerOptions, OpticalDiscOptions opticalDiscOptions, bool autoPlay)
        {
            // Stop current playback, if necessary
            if(PlayerState != PlayerState.NoDisc)
                ExecuteStop();

            // Attempt to initialize Player
            _player.Init(path, playerOptions, opticalDiscOptions, autoPlay);
            if(_player.Initialized)
            {
                _player.PropertyChanged += PlayerStateChanged;
                PlayerStateChanged(this, null);
            }
        }

        #region Playback (UI)

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
        /// Shuffle the current track list
        /// </summary>
        public void ExecuteShuffle() => _player?.ShuffleTracks();

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void ExecuteStop() => _player?.Stop();

        /// <summary>
        /// Eject the currently loaded disc
        /// </summary>
        public void ExecuteEject() => _player?.Eject();

        /// <summary>
        /// Move to the next disc
        /// </summary>
        public void ExecuteNextDisc() => _player?.NextDisc();

        /// <summary>
        /// Move to the previous disc
        /// </summary>
        public void ExecutePreviousDisc() => _player?.PreviousDisc();

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

        #region Playback (Internal)

        /// <summary>
        /// Select a particular disc by number
        /// </summary>
        /// <param name="discNumber">Disc number to attempt to load</param>
        public void SelectDisc(int discNumber) => _player?.SelectDisc(discNumber);

        /// <summary>
        /// Select a particular index by number
        /// </summary>
        /// <param name="index">Track index to attempt to load</param>
        /// <param name="changeTrack">True if index changes can trigger a track change, false otherwise</param>
        public void SelectIndex(ushort index, bool changeTrack) => _player?.SelectIndex(index, changeTrack);

        /// <summary>
        /// Select a particular track by number
        /// </summary>
        /// <param name="trackNumber">Track number to attempt to load</param>
        public void SelectTrack(int trackNumber) => _player?.SelectTrack(trackNumber);

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

        #region Extraction

        /// <summary>
        /// Extract a single track from the image to WAV
        /// </summary>
        /// <param name="trackNumber"></param>
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractSingleTrackToWav(uint trackNumber, string outputDirectory) => _player?.ExtractSingleTrackToWav(trackNumber, outputDirectory);

        /// <summary>
        /// Extract all tracks from the image to WAV
        /// </summary>
        /// <param name="outputDirectory">Output path to write data to</param>
        public void ExtractAllTracksToWav(string outputDirectory) => _player?.ExtractAllTracksToWav(outputDirectory);

        #endregion

        #region Setters

        /// <summary>
        /// Set data playback method [CompactDisc only]
        /// </summary>
        /// <param name="dataPlayback">New playback value</param>
        public void SetDataPlayback(DataPlayback dataPlayback) => _player?.SetDataPlayback(dataPlayback);

        /// <summary>
        /// Set disc handling method
        /// </summary>
        /// <param name="discHandling">New playback value</param>
        public void SetDiscHandling(DiscHandling discHandling) => _player?.SetDiscHandling(discHandling);

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

        #endregion

        #region State Change Event Handlers

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

            ImagePath = _player.ImagePath;
            Initialized = _player.Initialized;

            if (!string.IsNullOrWhiteSpace(ImagePath) && Initialized)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    App.MainWindow.Title = "RedBookPlayer - " + ImagePath.Split('/').Last().Split('\\').Last();
                });
            }

            CurrentDisc = _player.CurrentDisc;
            CurrentTrackNumber = _player.CurrentTrackNumber;
            CurrentTrackIndex = _player.CurrentTrackIndex;
            CurrentTrackSession = _player.CurrentTrackSession;
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

        #endregion

        #region Helpers

        /// <summary>
        /// Apply a custom theme to the player
        /// </summary>
        /// <param name="theme">Path to the theme under the themes directory</param>
        public void ApplyTheme(string theme)
        {
            // If the PlayerView isn't set, don't do anything
            if(App.PlayerView == null)
                return;

            // If no theme path is provided, we can ignore
            if(string.IsNullOrWhiteSpace(theme))
                return;

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

            App.MainWindow.Width = App.PlayerView.Width;
            App.MainWindow.Height = App.PlayerView.Height;
            InitializeDigits();
        }

        /// <summary>
        /// Load a disc image from a selection box
        /// </summary>
        public async void ExecuteLoad()
        {
            string[] paths = await GetPaths();
            if(paths == null || paths.Length == 0)
            {
                return;
            }
            else if(paths.Length == 1)
            {
                await LoadImage(paths[0]);
            }
            else
            {
                int lastDisc = CurrentDisc;
                foreach(string path in paths)
                {
                    await LoadImage(path);
                    
                    if(Initialized)
                        ExecuteNextDisc();
                }

                SelectDisc(lastDisc);
            }
        }

        /// <summary>
        /// Initialize the displayed digits array
        /// </summary>
        public void InitializeDigits()
        {
            if(App.PlayerView == null)
                return;

            _digits = new Image[]
            {
                App.PlayerView.FindControl<Image>("TrackDigit1"),
                App.PlayerView.FindControl<Image>("TrackDigit2"),

                App.PlayerView.FindControl<Image>("IndexDigit1"),
                App.PlayerView.FindControl<Image>("IndexDigit2"),

                App.PlayerView.FindControl<Image>("TimeDigit1"),
                App.PlayerView.FindControl<Image>("TimeDigit2"),
                App.PlayerView.FindControl<Image>("TimeDigit3"),
                App.PlayerView.FindControl<Image>("TimeDigit4"),
                App.PlayerView.FindControl<Image>("TimeDigit5"),
                App.PlayerView.FindControl<Image>("TimeDigit6"),

                App.PlayerView.FindControl<Image>("TotalTracksDigit1"),
                App.PlayerView.FindControl<Image>("TotalTracksDigit2"),

                App.PlayerView.FindControl<Image>("TotalIndexesDigit1"),
                App.PlayerView.FindControl<Image>("TotalIndexesDigit2"),

                App.PlayerView.FindControl<Image>("TotalTimeDigit1"),
                App.PlayerView.FindControl<Image>("TotalTimeDigit2"),
                App.PlayerView.FindControl<Image>("TotalTimeDigit3"),
                App.PlayerView.FindControl<Image>("TotalTimeDigit4"),
                App.PlayerView.FindControl<Image>("TotalTimeDigit5"),
                App.PlayerView.FindControl<Image>("TotalTimeDigit6"),
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
                PlayerOptions playerOptions = new PlayerOptions
                {
                    DataPlayback = App.Settings.DataPlayback,
                    DiscHandling = App.Settings.DiscHandling,
                    LoadHiddenTracks = App.Settings.PlayHiddenTracks,
                    RepeatMode = App.Settings.RepeatMode,
                    SessionHandling = App.Settings.SessionHandling,
                };

                OpticalDiscOptions opticalDiscOptions = new OpticalDiscOptions
                {
                    GenerateMissingToc = App.Settings.GenerateMissingTOC,
                };

                // Ensure the context and view model are set
                App.PlayerView.DataContext = this;
                App.PlayerView.ViewModel = this;

                Init(path, playerOptions, opticalDiscOptions, App.Settings.AutoPlay);
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
            SetDiscHandling(App.Settings.DiscHandling);
            SetLoadHiddenTracks(App.Settings.PlayHiddenTracks);
            SetRepeatMode(App.Settings.RepeatMode);
            SetSessionHandling(App.Settings.SessionHandling);
        }

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
                string themeDirectory = $"{Directory.GetCurrentDirectory()}/themes/{App.Settings.SelectedTheme}";
                using FileStream stream = File.Open($"{themeDirectory}/{character}.png", FileMode.Open);
                return new Bitmap(stream);
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
        /// <returns>User-selected paths, if possible</returns>
        private async Task<string[]> GetPaths()
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new OpenFileDialog { AllowMultiple = true };
                List<string> knownExtensions = new Aaru.DiscImages.AaruFormat().KnownExtensions.ToList();
                dialog.Filters.Add(new FileDialogFilter()
                {
                    Name = "Aaru Image Format (*" + string.Join(", *", knownExtensions) + ")",
                    Extensions = knownExtensions.ConvertAll(e => e.TrimStart('.'))
                });

                return (await dialog.ShowAsync(App.MainWindow));
            });
        }

        /// <summary>
        /// Load the theme from a XAML, if possible
        /// </summary>
        /// <param name="xaml">XAML data representing the theme, null for default</param>
        private void LoadTheme(string xaml)
        {
            // If the view is null, we can't load the theme
            if(App.PlayerView == null)
                return;

            try
            {
                if(xaml != null)
                    new AvaloniaXamlLoader().Load(xaml, null, App.PlayerView);
                else
                    AvaloniaXamlLoader.Load(App.PlayerView);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex);
            }

            // Ensure the context and view model are set
            App.PlayerView.DataContext = this;
            App.PlayerView.ViewModel = this;
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