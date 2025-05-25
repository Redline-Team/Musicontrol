using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Linux
{
    /// <summary>
    /// Implementation of Rhythmbox music player control for Linux.
    /// </summary>
    public class RhythmboxPlayer : LinuxMusicPlayerBase
    {
        private const string DBUS_SERVICE = "org.gnome.Rhythmbox3";
        private const string DBUS_PATH = "/org/gnome/Rhythmbox3/Player";
        private const string DBUS_PLAYER_INTERFACE = "org.gnome.Rhythmbox3.Player";
        private const string DBUS_PROPERTIES_INTERFACE = "org.freedesktop.DBus.Properties";

        private TrackInfo _currentTrack = new TrackInfo();
        private bool _isPlaying = false;
        private float _volume = 0.5f;

        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        public override string PlayerName => "Rhythmbox";

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// </summary>
        public override bool IsRunning => IsProcessRunning("rhythmbox");

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

                try
                {
                    string result = SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "getPlaying");
                    _isPlaying = result.Contains("true");
                    return _isPlaying;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error checking if Rhythmbox is playing: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Play the current track.
        /// </summary>
        public override void Play()
        {
            if (IsRunning)
            {
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "playPause", "boolean:true");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "playPause", "boolean:false");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "next");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "previous");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "setVolume", $"double:{_volume}");
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
                try
                {
                    string result = SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "getVolume");
                    
                    // Parse the volume from the D-Bus output
                    int startIndex = result.IndexOf("double");
                    if (startIndex > 0)
                    {
                        string volumeStr = result.Substring(startIndex).Split(' ')[1].Trim();
                        if (float.TryParse(volumeStr, out float volume))
                        {
                            _volume = volume;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error getting Rhythmbox volume: {ex.Message}");
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
                // Get the current playing URI
                string uriResult = SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "getPlayingUri");
                string uri = ExtractStringValue(uriResult);
                
                if (!string.IsNullOrEmpty(uri))
                {
                    // Get song properties
                    string titleResult = ExecuteShellCommand($"rhythmbox-client --print-playing-format=%tt");
                    string artistResult = ExecuteShellCommand($"rhythmbox-client --print-playing-format=%ta");
                    string albumResult = ExecuteShellCommand($"rhythmbox-client --print-playing-format=%at");
                    string durationResult = ExecuteShellCommand($"rhythmbox-client --print-playing-format=%td");
                    
                    _currentTrack.Title = titleResult.Trim();
                    _currentTrack.Artist = artistResult.Trim();
                    _currentTrack.Album = albumResult.Trim();
                    
                    if (float.TryParse(durationResult, out float duration))
                    {
                        _currentTrack.Duration = duration;
                    }
                    
                    // Get position
                    string positionResult = ExecuteShellCommand($"rhythmbox-client --print-playing-format=%te");
                    if (float.TryParse(positionResult, out float position))
                    {
                        _currentTrack.Position = position;
                    }
                    
                    _isPlaying = true;
                }
                else
                {
                    _isPlaying = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating Rhythmbox track info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extracts a string value from a D-Bus output line.
        /// </summary>
        private string ExtractStringValue(string output)
        {
            int valueStartIndex = output.IndexOf("string");
            if (valueStartIndex > 0)
            {
                string valueWithQuotes = output.Substring(valueStartIndex + 6).Trim();
                
                // Remove quotes if present
                if (valueWithQuotes.StartsWith("\"") && valueWithQuotes.EndsWith("\""))
                {
                    return valueWithQuotes.Substring(1, valueWithQuotes.Length - 2);
                }
                
                return valueWithQuotes;
            }
            
            return string.Empty;
        }
    }
}
