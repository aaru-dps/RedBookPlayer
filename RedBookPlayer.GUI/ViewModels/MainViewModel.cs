using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using RedBookPlayer.GUI.Views;

namespace RedBookPlayer.GUI.ViewModels
{
    public class MainViewModel
    {
        /// <summary>
        /// Read-only access to the control
        /// </summary>
        public ContentControl ContentControl => App.MainWindow.FindControl<ContentControl>("Content");

        /// <summary>
        /// Read-only access to the view
        /// </summary>
        public PlayerView PlayerView => ContentControl?.Content as PlayerView;

        #region Helpers

        /// <summary>
        /// Execute the result of a keypress
        /// </summary>
        public async void ExecuteKeyPress(object sender, KeyEventArgs e)
        {
            // Open settings window
            if(e.Key == App.Settings.OpenSettingsKey)
            {
                SettingsWindow settingsWindow = new SettingsWindow() { DataContext = App.Settings };
                settingsWindow.Closed += OnSettingsClosed;
                settingsWindow.ShowDialog(App.MainWindow);
            }

            // Load image
            else if(e.Key == App.Settings.LoadImageKey)
            {
                PlayerView?.ViewModel?.ExecuteLoad();
            }

            // Toggle playback
            else if(e.Key == App.Settings.TogglePlaybackKey || e.Key == Key.MediaPlayPause)
            {
                PlayerView?.ViewModel?.ExecuteTogglePlayPause();
            }

            // Stop playback
            else if(e.Key == App.Settings.StopPlaybackKey || e.Key == Key.MediaStop)
            {
                PlayerView?.ViewModel?.ExecuteStop();
            }

            // Next Track
            else if(e.Key == App.Settings.NextTrackKey || e.Key == Key.MediaNextTrack)
            {
                PlayerView?.ViewModel?.ExecuteNextTrack();
            }

            // Previous Track
            else if(e.Key == App.Settings.PreviousTrackKey || e.Key == Key.MediaPreviousTrack)
            {
                PlayerView?.ViewModel?.ExecutePreviousTrack();
            }

            // Next Index
            else if(e.Key == App.Settings.NextIndexKey)
            {
                PlayerView?.ViewModel?.ExecuteNextIndex();
            }

            // Previous Index
            else if(e.Key == App.Settings.PreviousIndexKey)
            {
                PlayerView?.ViewModel?.ExecutePreviousIndex();
            }

            // Fast Foward
            else if(e.Key == App.Settings.FastForwardPlaybackKey)
            {
                PlayerView?.ViewModel?.ExecuteFastForward();
            }

            // Rewind
            else if(e.Key == App.Settings.RewindPlaybackKey)
            {
                PlayerView?.ViewModel?.ExecuteRewind();
            }

            // Volume Up
            else if(e.Key == App.Settings.VolumeUpKey || e.Key == Key.VolumeUp)
            {
                int increment = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    increment *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    increment *= 5;

                if(PlayerView?.ViewModel?.Volume != null)
                    PlayerView.ViewModel.ExecuteSetVolume(PlayerView.ViewModel.Volume + increment);
            }

            // Volume Down
            else if(e.Key == App.Settings.VolumeDownKey || e.Key == Key.VolumeDown)
            {
                int decrement = 1;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Control))
                    decrement *= 2;
                if(e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    decrement *= 5;

                if(PlayerView?.ViewModel?.Volume != null)
                    PlayerView.ViewModel.ExecuteSetVolume(PlayerView.ViewModel.Volume - decrement);
            }

            // Mute Toggle
            else if(e.Key == App.Settings.ToggleMuteKey || e.Key == Key.VolumeMute)
            {
                PlayerView?.ViewModel?.ExecuteToggleMute();
            }

            // Emphasis Toggle
            else if(e.Key == App.Settings.ToggleDeEmphasisKey)
            {
                PlayerView?.ViewModel?.ExecuteToggleDeEmphasis();
            }
        }

        /// <summary>
        /// Load the first valid drag-and-dropped disc image
        /// </summary>
        public async void ExecuteLoadDragDrop(object sender, DragEventArgs e)
        {
            if(PlayerView?.ViewModel == null)
                return;

            IEnumerable<string> fileNames = e.Data.GetFileNames();
            foreach(string filename in fileNames)
            {
                bool loaded = await PlayerView.ViewModel.LoadImage(filename);
                if(loaded)
                    break;
            }
        }

        /// <summary>
        /// Stop current playback
        /// </summary>
        public void ExecuteStop(object sender, CancelEventArgs e) => PlayerView?.ViewModel?.ExecuteStop();

        /// <summary>
        /// Handle the settings window closing
        /// </summary>
        private void OnSettingsClosed(object sender, EventArgs e) => PlayerView?.ViewModel?.RefreshFromSettings();

        #endregion
    }
}