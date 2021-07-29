using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;
using RedBookPlayer.Models;

namespace RedBookPlayer.GUI.ViewModels
{
    public class SettingsViewModel : ReactiveObject
    {
        #region Player Settings

        /// <summary>
        /// List of all data playback values
        /// </summary>
        [JsonIgnore]
        public List<DataPlayback> DataPlaybackValues => GenerateDataPlaybackList();

        /// <summary>
        /// List of all session handling values
        /// </summary>
        [JsonIgnore]
        public List<SessionHandling> SessionHandlingValues => GenerateSessionHandlingList();

        /// <summary>
        /// List of all themes
        /// </summary>
        [JsonIgnore]
        public List<string> ThemeValues => GenerateThemeList();

        /// <summary>
        /// Indicates if discs should start playing on load
        /// </summary>
        public bool AutoPlay { get; set; } = false;

        /// <summary>
        /// Indicates if an index change can trigger a track change
        /// </summary>
        public bool IndexButtonChangeTrack { get; set; } = false;

        /// <summary>
        /// Indicates if hidden tracks should be played
        /// </summary>
        /// <remarks>
        /// Hidden tracks can be one of the following:
        /// - TrackSequence == 0
        /// - Larget pregap of track 1 (> 150 sectors)
        /// </remarks>
        public bool PlayHiddenTracks { get; set; } = false;

        /// <summary>
        /// Generate a TOC if the disc is missing one
        /// </summary>
        public bool GenerateMissingTOC { get; set; } = true;

        /// <summary>
        /// Indicates how to deal with data tracks
        /// </summary>
        public DataPlayback DataPlayback { get; set; } = DataPlayback.Skip;

        /// <summary>
        /// Indicates how to repeat tracks
        /// </summary>
        /// TODO: Add to settings UI
        public RepeatMode RepeatMode { get; set; } = RepeatMode.All;

        /// <summary>
        /// Indicates how to handle tracks on different sessions
        /// </summary>
        public SessionHandling SessionHandling { get; set; } = SessionHandling.FirstSessionOnly;

        /// <summary>
        /// Indicates the default playback volume
        /// </summary>
        public int Volume
        {
            get => _volume;
            set
            {
                if(value > 100)
                    _volume = 100;
                else if(value < 0)
                    _volume = 0;
                else
                    _volume = value;
            }
        }

        /// <summary>
        /// Indicates the currently selected theme
        /// </summary>
        public string SelectedTheme { get; set; } = "default";

        #endregion

        #region Key Mappings

        /// <summary>
        /// List of all keyboard keys
        /// </summary>
        [JsonIgnore]
        public List<Key> KeyboardList => GenerateKeyList();

        /// <summary>
        /// Key assigned to open settings
        /// </summary>
        public Key OpenSettingsKey { get; set; } = Key.F1;

        /// <summary>
        /// Key assigned to load a new image
        /// </summary>
        public Key LoadImageKey { get; set; } = Key.F2;

        /// <summary>
        /// Key assigned to toggle play and pause
        /// </summary>
        public Key TogglePlaybackKey { get; set; } = Key.Space;

        /// <summary>
        /// Key assigned to stop playback
        /// </summary>
        public Key StopPlaybackKey { get; set; } = Key.Escape;

        /// <summary>
        /// Key assigned to eject the disc
        /// </summary>
        public Key EjectKey { get; set; } = Key.OemTilde;

        /// <summary>
        /// Key assigned to move to the next track
        /// </summary>
        public Key NextTrackKey { get; set; } = Key.Right;

        /// <summary>
        /// Key assigned to move to the previous track
        /// </summary>
        public Key PreviousTrackKey { get; set; } = Key.Left;

        /// <summary>
        /// Key assigned to move to the next index
        /// </summary>
        public Key NextIndexKey { get; set; } = Key.OemCloseBrackets;

        /// <summary>
        /// Key assigned to move to the previous index
        /// </summary>
        public Key PreviousIndexKey { get; set; } = Key.OemOpenBrackets;

        /// <summary>
        /// Key assigned to fast forward playback
        /// </summary>
        public Key FastForwardPlaybackKey { get; set; } = Key.OemPeriod;

        /// <summary>
        /// Key assigned to rewind playback
        /// </summary>
        public Key RewindPlaybackKey { get; set; } = Key.OemComma;

        /// <summary>
        /// Key assigned to raise volume
        /// </summary>
        public Key VolumeUpKey { get; set; } = Key.Add;

        /// <summary>
        /// Key assigned to lower volume
        /// </summary>
        public Key VolumeDownKey { get; set; } = Key.Subtract;

        /// <summary>
        /// Key assigned to toggle mute
        /// </summary>
        public Key ToggleMuteKey { get; set; } = Key.M;

        /// <summary>
        /// Key assigned to toggle de-emphasis
        /// </summary>
        public Key ToggleDeEmphasisKey { get; set; } = Key.E;

        #endregion

        #region Commands

        /// <summary>
        /// Command for applying settings
        /// </summary>
        public ReactiveCommand<Unit, Unit> ApplySettingsCommand { get; }

        /// <summary>
        /// Command for changing the volume
        /// </summary>
        public ReactiveCommand<Unit, Unit> VolumeChangedCommand { get; }

        #endregion

        /// <summary>
        /// Path to the settings file
        /// </summary>
        private string _filePath;

        /// <summary>
        /// Internal value for the volume
        /// </summary>
        private int _volume = 100;

        public SettingsViewModel() : this(null) { }

        public SettingsViewModel(string filePath)
        {
            _filePath = filePath;

            // Intialize commands
            ApplySettingsCommand = ReactiveCommand.Create(ExecuteApplySettings);
            VolumeChangedCommand = ReactiveCommand.Create(ExecuteVolumeChanged);
        }

        /// <summary>
        /// Load settings from a file
        /// </summary>
        /// <param name="filePath">Path to the settings JSON file</param>
        /// <returns>Settings derived from the input file, if possible</returns>
        public static SettingsViewModel Load(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    SettingsViewModel settings = JsonSerializer.Deserialize<SettingsViewModel>(File.ReadAllText(filePath));
                    settings._filePath = filePath;

                    return settings;
                }
                catch(JsonException)
                {
                    Console.WriteLine("Couldn't parse settings, reverting to default");

                    return new SettingsViewModel(filePath);
                }
            }

            return new SettingsViewModel(filePath);
        }

        /// <summary>
        /// Apply settings from the UI
        /// </summary>
        public void ExecuteApplySettings()
        {
            if(!string.IsNullOrWhiteSpace(SelectedTheme))
                App.MainWindow.PlayerView?.PlayerViewModel?.ApplyTheme(SelectedTheme);

            Save();
        }

        /// <summary>
        /// Handle volume changing
        /// </summary>
        public void ExecuteVolumeChanged()
        {
            try
            {
                TextBlock volumeLabel = App.MainWindow.SettingsWindow.FindControl<TextBlock>("VolumeLabel");
                if(volumeLabel != null)
                    volumeLabel.Text = Volume.ToString();
            }
            catch { }
        }

        /// <summary>
        /// Save settings to a file
        /// </summary>
        private void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(_filePath, json);
        }

        #region Generation

        /// <summary>
        /// Generate the list of DataPlayback values
        /// </summary>
        private List<DataPlayback> GenerateDataPlaybackList() => Enum.GetValues(typeof(DataPlayback)).Cast<DataPlayback>().ToList();

        /// <summary>
        /// Generate the list of Key values
        /// </summary>
        private List<Key> GenerateKeyList() => Enum.GetValues(typeof(Key)).Cast<Key>().ToList();

        /// <summary>
        /// Generate the list of SessionHandling values
        /// </summary>
        private List<SessionHandling> GenerateSessionHandlingList() => Enum.GetValues(typeof(SessionHandling)).Cast<SessionHandling>().ToList();

        /// <summary>
        /// Generate the list of valid themes
        /// </summary>
        private List<string> GenerateThemeList()
        {
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

            return items;
        }

        #endregion
    }
}