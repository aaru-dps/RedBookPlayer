using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using RedBookPlayer.GUI.ViewModels;

namespace RedBookPlayer.GUI.Views
{
    public class MainWindow : Window
    {
        public static MainWindow     Instance;
        public        ContentControl ContentControl;
        public        Window         settingsWindow;

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the main window
        /// </summary>
        void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            ContentControl         = this.FindControl<ContentControl>("Content");
            ContentControl.Content = new PlayerView();

            Instance.MaxWidth  = ((PlayerView)Instance.ContentControl.Content).Width;
            Instance.MaxHeight = ((PlayerView)Instance.ContentControl.Content).Height;

            ContentControl.Content = new PlayerView();
            ((PlayerView)Instance.ContentControl.Content).PlayerViewModel.ApplyTheme(App.Settings.SelectedTheme);

            CanResize = false;

            KeyDown += OnKeyDown;

            Closing += (s, e) =>
            {
                settingsWindow?.Close();
                settingsWindow = null;
            };

            Closing += (e, f) =>
            {
                ((PlayerView)ContentControl.Content).PlayerViewModel.ExecuteStop();
            };

            AddHandler(DragDrop.DropEvent, MainWindow_Drop);
        }

        #region Event Handlers

        public async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            PlayerView playerView = ContentControl.Content as PlayerView;
            if(playerView == null)
                return;

            IEnumerable<string> fileNames = e.Data.GetFileNames();
            foreach(string filename in fileNames)
            {
                bool loaded = await playerView?.PlayerViewModel?.LoadImage(filename);
                if(loaded)
                    break;
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            PlayerView playerView = ContentControl.Content as PlayerView;

            // Open settings window
            if(e.Key == App.Settings.OpenSettingsKey)
            {
                settingsWindow = new SettingsWindow(App.Settings);
                settingsWindow.Closed += OnSettingsClosed;
                settingsWindow.Show();
            }

            // Load image
            else if (e.Key == App.Settings.LoadImageKey)
            {
                playerView?.PlayerViewModel?.ExecuteLoad();
            }

            // Toggle playback
            else if(e.Key == App.Settings.TogglePlaybackKey || e.Key == Key.MediaPlayPause)
            {
                playerView?.PlayerViewModel?.ExecuteTogglePlayPause();
            }

            // Stop playback
            else if(e.Key == App.Settings.StopPlaybackKey || e.Key == Key.MediaStop)
            {
                playerView?.PlayerViewModel?.ExecuteStop();
            }

            // Eject
            else if(e.Key == App.Settings.EjectKey)
            {
                playerView?.PlayerViewModel?.ExecuteEject();
            }

            // Next Track
            else if(e.Key == App.Settings.NextTrackKey || e.Key == Key.MediaNextTrack)
            {
                playerView?.PlayerViewModel?.ExecuteNextTrack();
            }

            // Previous Track
            else if(e.Key == App.Settings.PreviousTrackKey || e.Key == Key.MediaPreviousTrack)
            {
                playerView?.PlayerViewModel?.ExecutePreviousTrack();
            }

            // Next Index
            else if(e.Key == App.Settings.NextIndexKey)
            {
                playerView?.PlayerViewModel?.ExecuteNextIndex();
            }

            // Previous Index
            else if(e.Key == App.Settings.PreviousIndexKey)
            {
                playerView?.PlayerViewModel?.ExecutePreviousIndex();
            }

            // Fast Foward
            else if(e.Key == App.Settings.FastForwardPlaybackKey)
            {
                playerView?.PlayerViewModel?.ExecuteFastForward();
            }

            // Rewind
            else if(e.Key == App.Settings.RewindPlaybackKey)
            {
                playerView?.PlayerViewModel?.ExecuteRewind();
            }

            // Volume Up
            else if(e.Key == App.Settings.VolumeUpKey || e.Key == Key.VolumeUp)
            {
                int increment = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    increment *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    increment *= 5;

                if(playerView?.PlayerViewModel?.Volume != null)
                    playerView.PlayerViewModel.ExecuteSetVolume(playerView.PlayerViewModel.Volume + increment);
            }

            // Volume Down
            else if(e.Key == App.Settings.VolumeDownKey || e.Key == Key.VolumeDown)
            {
                int decrement = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    decrement *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    decrement *= 5;

                if (playerView?.PlayerViewModel?.Volume != null)
                    playerView.PlayerViewModel.ExecuteSetVolume(playerView.PlayerViewModel.Volume - decrement);
            }

            // Mute Toggle
            else if(e.Key == App.Settings.ToggleMuteKey || e.Key == Key.VolumeMute)
            {
                playerView?.PlayerViewModel?.ExecuteToggleMute();
            }

            // Emphasis Toggle
            else if(e.Key == App.Settings.ToggleDeEmphasisKey)
            {
                playerView?.PlayerViewModel?.ExecuteToggleDeEmphasis();
            }
        }

        public void OnSettingsClosed(object sender, EventArgs e)
        {
            PlayerView playerView = ContentControl.Content as PlayerView;
            playerView?.UpdateViewModel();
        }

        #endregion
    }
}