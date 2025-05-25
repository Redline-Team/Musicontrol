using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.MacOS
{
    /// <summary>
    /// Implementation of Spotify music player control for macOS.
    /// </summary>
    public class SpotifyMacOSPlayer : MacOSMusicPlayerBase
    {
        private TrackInfo _currentTrack = new TrackInfo();
        private bool _isPlaying = false;
        private float _volume = 0.5f;

        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        public override string PlayerName => "Spotify";

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// </summary>
        public override bool IsRunning => IsApplicationRunning("Spotify");

        /// <summary>
        /// Gets the current track information.
        /// </summary>
        public override TrackInfo CurrentTrack
        {
            get
            {
                UpdateTrackInfo();
                return _currentTrack;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the player is currently playing.
        /// </summary>
        public override bool IsPlaying
        {
            get
            {
                if (!IsRunning)
                    return false;

                string script = "tell application \"Spotify\" to player state as string";
                string result = ExecuteAppleScript(script);
                _isPlaying = result.ToLower() == "playing";
                return _isPlaying;
            }
        }

        /// <summary>
        /// Play the current track.
        /// </summary>
        public override void Play()
        {
            if (IsRunning)
            {
                ExecuteAppleScript("tell application \"Spotify\" to play");
                _isPlaying = true;
            }
        }

        /// <summary>
        /// Pause the current track.
        /// </summary>
        public override void Pause()
        {
            if (IsRunning)
            {
                ExecuteAppleScript("tell application \"Spotify\" to pause");
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Play the next track.
        /// </summary>
        public override void Next()
        {
            if (IsRunning)
            {
                ExecuteAppleScript("tell application \"Spotify\" to next track");
                UpdateTrackInfo();
            }
        }

        /// <summary>
        /// Play the previous track.
        /// </summary>
        public override void Previous()
        {
            if (IsRunning)
            {
                ExecuteAppleScript("tell application \"Spotify\" to previous track");
                UpdateTrackInfo();
            }
        }

        /// <summary>
        /// Set the volume level.
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0).</param>
        public override void SetVolume(float volume)
        {
            if (IsRunning)
            {
                _volume = Mathf.Clamp01(volume);
                int spotifyVolume = Mathf.RoundToInt(_volume * 100);
                ExecuteAppleScript($"tell application \"Spotify\" to set sound volume to {spotifyVolume}");
            }
        }

        /// <summary>
        /// Get the current volume level.
        /// </summary>
        /// <returns>Volume level (0.0 to 1.0).</returns>
        public override float GetVolume()
        {
            if (IsRunning)
            {
                string script = "tell application \"Spotify\" to sound volume as string";
                string result = ExecuteAppleScript(script);
                
                if (int.TryParse(result, out int volume))
                {
                    _volume = volume / 100f;
                }
            }
            
            return _volume;
        }

        /// <summary>
        /// Updates the current track information.
        /// </summary>
        private void UpdateTrackInfo()
        {
            if (!IsRunning)
                return;

            try
            {
                string script = @"
                tell application ""Spotify""
                    if player state is playing then
                        set trackName to name of current track
                        set trackArtist to artist of current track
                        set trackAlbum to album of current track
                        set trackDuration to duration of current track
                        set trackPosition to player position
                        return trackName & "":::"" & trackArtist & "":::"" & trackAlbum & "":::"" & trackDuration & "":::"" & trackPosition
                    else
                        return """"
                    end if
                end tell";

                string result = ExecuteAppleScript(script);
                
                if (!string.IsNullOrEmpty(result))
                {
                    string[] parts = result.Split(new string[] { ":::" }, StringSplitOptions.None);
                    
                    if (parts.Length >= 5)
                    {
                        _currentTrack.Title = parts[0];
                        _currentTrack.Artist = parts[1];
                        _currentTrack.Album = parts[2];
                        
                        if (float.TryParse(parts[3], out float duration))
                            _currentTrack.Duration = duration / 1000f; // Spotify returns duration in milliseconds
                        
                        if (float.TryParse(parts[4], out float position))
                            _currentTrack.Position = position;
                        
                        _isPlaying = true;
                    }
                }
                else
                {
                    _isPlaying = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating Spotify track info: {ex.Message}");
            }
        }
    }
}
