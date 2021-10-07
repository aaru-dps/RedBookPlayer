namespace RedBookPlayer.Models.Audio
{
    public interface IAudioBackend
    {
        /// <summary>
        /// Pauses the audio playback
        /// </summary>
        void Pause();

        /// <summary>
        /// Starts the playback.
        /// </summary>
        void Play();

        /// <summary>
        /// Stops the audio playback
        /// </summary>
        void Stop();

        /// <summary>
        /// Get the current playback state
        /// </summary>
        PlayerState GetPlayerState();

        /// <summary>
        /// Set the new volume value
        /// </summary>
        void SetVolume(float volume);
    }
}