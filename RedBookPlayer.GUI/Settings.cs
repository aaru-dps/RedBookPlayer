using System;
using System.IO;
using System.Text.Json;
using Avalonia.Input;
using RedBookPlayer.GUI.Views;

namespace RedBookPlayer.GUI
{
    public class Settings
    {
        #region Player Settings

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
        /// Indicates if data tracks should be played like old, non-compliant players
        /// </summary>
        public bool PlayDataTracks { get; set; } = false;

        /// <summary>
        /// Generate a TOC if the disc is missing one
        /// </summary>
        public bool GenerateMissingTOC { get; set; } = true;

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

        /// <summary>
        /// Path to the settings file
        /// </summary>
        private string _filePath;

        /// <summary>
        /// Internal value for the volume
        /// </summary>
        private int _volume = 100;

        public Settings() {}

        public Settings(string filePath) => _filePath = filePath;

        /// <summary>
        /// Load settings from a file
        /// </summary>
        /// <param name="filePath">Path to the settings JSON file</param>
        /// <returns>Settings derived from the input file, if possible</returns>
        public static Settings Load(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(filePath));
                    settings._filePath = filePath;

                    MainWindow.ApplyTheme(settings.SelectedTheme);

                    return settings;
                }
                catch(JsonException)
                {
                    Console.WriteLine("Couldn't parse settings, reverting to default");

                    return new Settings(filePath);
                }
            }

            return new Settings(filePath);
        }

        /// <summary>
        /// Save settings to a file
        /// </summary>
        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(_filePath, json);
        }
    }
}