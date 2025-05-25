using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Linux
{
    /// <summary>
    /// Implementation of VLC media player control for Linux.
    /// </summary>
    public class VlcPlayer : LinuxMusicPlayerBase
    {
        private const string DBUS_SERVICE = "org.mpris.MediaPlayer2.vlc";
        private const string DBUS_PATH = "/org/mpris/MediaPlayer2";
        private const string DBUS_PLAYER_INTERFACE = "org.mpris.MediaPlayer2.Player";
        private const string DBUS_PROPERTIES_INTERFACE = "org.freedesktop.DBus.Properties";

        private TrackInfo _currentTrack = new TrackInfo();
        private bool _isPlaying = false;
        private float _volume = 0.5f;

        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        public override string PlayerName => "VLC";

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// </summary>
        public override bool IsRunning => IsProcessRunning("vlc");

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
                    string command = $"dbus-send --print-reply --dest={DBUS_SERVICE} {DBUS_PATH} {DBUS_PROPERTIES_INTERFACE}.Get string:'{DBUS_PLAYER_INTERFACE}' string:'PlaybackStatus'";
                    string result = ExecuteShellCommand(command);
                    _isPlaying = result.Contains("Playing");
                    return _isPlaying;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error checking if VLC is playing: {ex.Message}");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "Play");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "Pause");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "Next");
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
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PLAYER_INTERFACE, "Previous");
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
                string args = $"double:{_volume}";
                SendDBusCommand(DBUS_SERVICE, DBUS_PATH, DBUS_PROPERTIES_INTERFACE, "Set", $"string:'{DBUS_PLAYER_INTERFACE}' string:'Volume' variant:{args}");
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
                    string command = $"dbus-send --print-reply --dest={DBUS_SERVICE} {DBUS_PATH} {DBUS_PROPERTIES_INTERFACE}.Get string:'{DBUS_PLAYER_INTERFACE}' string:'Volume'";
                    string result = ExecuteShellCommand(command);
                    
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
                    Debug.LogError($"Error getting VLC volume: {ex.Message}");
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
                string command = $"dbus-send --print-reply --dest={DBUS_SERVICE} {DBUS_PATH} {DBUS_PROPERTIES_INTERFACE}.Get string:'{DBUS_PLAYER_INTERFACE}' string:'Metadata'";
                string result = ExecuteShellCommand(command);
                
                if (!string.IsNullOrEmpty(result))
                {
                    // Parse title
                    int titleIndex = result.IndexOf("xesam:title");
                    if (titleIndex > 0)
                    {
                        string titleLine = result.Substring(titleIndex).Split('\n')[0];
                        _currentTrack.Title = ExtractStringValue(titleLine);
                    }
                    else
                    {
                        // Try to get from filename if title is not available
                        int urlIndex = result.IndexOf("xesam:url");
                        if (urlIndex > 0)
                        {
                            string urlLine = result.Substring(urlIndex).Split('\n')[0];
                            string url = ExtractStringValue(urlLine);
                            string fileName = System.IO.Path.GetFileNameWithoutExtension(url);
                            _currentTrack.Title = fileName;
                        }
                    }
                    
                    // Parse artist
                    int artistIndex = result.IndexOf("xesam:artist");
                    if (artistIndex > 0)
                    {
                        string artistSection = result.Substring(artistIndex);
                        int arrayStartIndex = artistSection.IndexOf("array [");
                        if (arrayStartIndex > 0)
                        {
                            string artistLine = artistSection.Substring(arrayStartIndex).Split('\n')[1];
                            _currentTrack.Artist = ExtractStringValue(artistLine);
                        }
                    }
                    
                    // Parse album
                    int albumIndex = result.IndexOf("xesam:album");
                    if (albumIndex > 0)
                    {
                        string albumLine = result.Substring(albumIndex).Split('\n')[0];
                        _currentTrack.Album = ExtractStringValue(albumLine);
                    }
                    
                    // Parse duration
                    int durationIndex = result.IndexOf("mpris:length");
                    if (durationIndex > 0)
                    {
                        string durationLine = result.Substring(durationIndex).Split('\n')[0];
                        int valueStartIndex = durationLine.IndexOf("int64") + 5;
                        string durationStr = durationLine.Substring(valueStartIndex).Trim();
                        if (long.TryParse(durationStr, out long duration))
                        {
                            _currentTrack.Duration = duration / 1000000f; // Convert microseconds to seconds
                        }
                    }
                    
                    // Get position separately
                    command = $"dbus-send --print-reply --dest={DBUS_SERVICE} {DBUS_PATH} {DBUS_PROPERTIES_INTERFACE}.Get string:'{DBUS_PLAYER_INTERFACE}' string:'Position'";
                    result = ExecuteShellCommand(command);
                    
                    int positionIndex = result.IndexOf("int64");
                    if (positionIndex > 0)
                    {
                        string positionStr = result.Substring(positionIndex + 5).Trim();
                        if (long.TryParse(positionStr, out long position))
                        {
                            _currentTrack.Position = position / 1000000f; // Convert microseconds to seconds
                        }
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
                Debug.LogError($"Error updating VLC track info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extracts a string value from a D-Bus output line.
        /// </summary>
        private string ExtractStringValue(string line)
        {
            int valueStartIndex = line.IndexOf("string") + 6;
            string valueWithQuotes = line.Substring(valueStartIndex).Trim();
            
            // Remove quotes if present
            if (valueWithQuotes.StartsWith("\"") && valueWithQuotes.EndsWith("\""))
            {
                return valueWithQuotes.Substring(1, valueWithQuotes.Length - 2);
            }
            
            return valueWithQuotes;
        }
    }
}
