using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime
{
    /// <summary>
    /// A manually created music player for when automatic detection fails.
    /// Particularly useful for Linux with Wayland where D-Bus detection might be unreliable.
    /// </summary>
    public class ManualMusicPlayer : IMusicPlayer
    {
        private string _playerName;
        private TrackInfo _currentTrack = new TrackInfo();
        private bool _isPlaying = false;
        private float _volume = 0.5f;
        private string _playerType;

        /// <summary>
        /// Creates a new instance of the ManualMusicPlayer class.
        /// </summary>
        /// <param name="playerType">The type of music player (e.g., "Spotify", "VLC").</param>
        public ManualMusicPlayer(string playerType)
        {
            _playerType = playerType;
            _playerName = $"{playerType} (Manual)";
            _currentTrack.Title = "Unknown";
            _currentTrack.Artist = "Unknown";
            _currentTrack.Album = "Unknown";
            
            // Try to update track info immediately
            UpdateTrackInfo();
        }

        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        public string PlayerName => _playerName;

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// For manual players, we always return true.
        /// </summary>
        public bool IsRunning => true;

        /// <summary>
        /// Gets the current track information.
        /// </summary>
        public TrackInfo CurrentTrack => _currentTrack;

        /// <summary>
        /// Gets a value indicating whether the player is currently playing.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Play the current track.
        /// </summary>
        public void Play()
        {
            try
            {
                string command = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Play";
                        break;
                    case "vlc":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Play";
                        break;
                    case "rhythmbox":
                        command = "dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.playPause boolean:true";
                        break;
                    default:
                        // Try playerctl as a fallback
                        command = $"playerctl -p {_playerType.ToLower()} play";
                        break;
                }
                
                ExecuteCommand(command);
                _isPlaying = true;
                
                // Try to update track info
                UpdateTrackInfo();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error playing {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Pause the current track.
        /// </summary>
        public void Pause()
        {
            try
            {
                string command = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Pause";
                        break;
                    case "vlc":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Pause";
                        break;
                    case "rhythmbox":
                        command = "dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.playPause boolean:false";
                        break;
                    default:
                        // Try playerctl as a fallback
                        command = $"playerctl -p {_playerType.ToLower()} pause";
                        break;
                }
                
                ExecuteCommand(command);
                _isPlaying = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error pausing {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Play the next track.
        /// </summary>
        public void Next()
        {
            try
            {
                string command = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Next";
                        break;
                    case "vlc":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Next";
                        break;
                    case "rhythmbox":
                        command = "dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.next";
                        break;
                    default:
                        // Try playerctl as a fallback
                        command = $"playerctl -p {_playerType.ToLower()} next";
                        break;
                }
                
                ExecuteCommand(command);
                
                // Try to update track info
                UpdateTrackInfo();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error skipping to next track for {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Play the previous track.
        /// </summary>
        public void Previous()
        {
            try
            {
                string command = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Previous";
                        break;
                    case "vlc":
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.mpris.MediaPlayer2.Player.Previous";
                        break;
                    case "rhythmbox":
                        command = "dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.previous";
                        break;
                    default:
                        // Try playerctl as a fallback
                        command = $"playerctl -p {_playerType.ToLower()} previous";
                        break;
                }
                
                ExecuteCommand(command);
                
                // Try to update track info
                UpdateTrackInfo();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error skipping to previous track for {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Set the volume level.
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0).</param>
        public void SetVolume(float volume)
        {
            try
            {
                _volume = Mathf.Clamp01(volume);
                string command = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        command = $"dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Set string:'org.mpris.MediaPlayer2.Player' string:'Volume' variant:double:{_volume}";
                        break;
                    case "vlc":
                        command = $"dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Set string:'org.mpris.MediaPlayer2.Player' string:'Volume' variant:double:{_volume}";
                        break;
                    case "rhythmbox":
                        command = $"dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.setVolume double:{_volume}";
                        break;
                    default:
                        // Try playerctl as a fallback
                        command = $"playerctl -p {_playerType.ToLower()} volume {_volume}";
                        break;
                }
                
                ExecuteCommand(command);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting volume for {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the current volume level.
        /// </summary>
        /// <returns>Volume level (0.0 to 1.0).</returns>
        public float GetVolume()
        {
            return _volume;
        }

        /// <summary>
        /// Updates the current track information.
        /// </summary>
        private void UpdateTrackInfo()
        {
            try
            {
                string command = "";
                string result = "";
                
                switch (_playerType.ToLower())
                {
                    case "spotify":
                        // Get metadata
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:'org.mpris.MediaPlayer2.Player' string:'Metadata'";
                        result = ExecuteCommand(command);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            ParseMetadata(result);
                        }
                        
                        // Check if playing
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:'org.mpris.MediaPlayer2.Player' string:'PlaybackStatus'";
                        result = ExecuteCommand(command);
                        _isPlaying = result.Contains("Playing");
                        break;
                        
                    case "vlc":
                        // Get metadata
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:'org.mpris.MediaPlayer2.Player' string:'Metadata'";
                        result = ExecuteCommand(command);
                        
                        if (!string.IsNullOrEmpty(result))
                        {
                            ParseMetadata(result);
                        }
                        
                        // Check if playing
                        command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.vlc /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:'org.mpris.MediaPlayer2.Player' string:'PlaybackStatus'";
                        result = ExecuteCommand(command);
                        _isPlaying = result.Contains("Playing");
                        break;
                        
                    case "rhythmbox":
                        // Try to get current track info using rhythmbox-client
                        string title = ExecuteCommand("rhythmbox-client --print-playing-format=%tt");
                        string artist = ExecuteCommand("rhythmbox-client --print-playing-format=%ta");
                        string album = ExecuteCommand("rhythmbox-client --print-playing-format=%at");
                        
                        if (!string.IsNullOrEmpty(title))
                        {
                            _currentTrack.Title = title;
                            _currentTrack.Artist = artist;
                            _currentTrack.Album = album;
                        }
                        
                        // Check if playing
                        result = ExecuteCommand("dbus-send --print-reply --dest=org.gnome.Rhythmbox3 /org/gnome/Rhythmbox3/Player org.gnome.Rhythmbox3.Player.getPlaying");
                        _isPlaying = result.Contains("true");
                        break;
                        
                    default:
                        // Try playerctl as a fallback
                        title = ExecuteCommand($"playerctl -p {_playerType.ToLower()} metadata title");
                        artist = ExecuteCommand($"playerctl -p {_playerType.ToLower()} metadata artist");
                        album = ExecuteCommand($"playerctl -p {_playerType.ToLower()} metadata album");
                        
                        if (!string.IsNullOrEmpty(title))
                        {
                            _currentTrack.Title = title;
                            _currentTrack.Artist = artist;
                            _currentTrack.Album = album;
                        }
                        
                        // Check if playing
                        result = ExecuteCommand($"playerctl -p {_playerType.ToLower()} status");
                        _isPlaying = result.Contains("Playing");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating track info for {_playerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses metadata from D-Bus output.
        /// </summary>
        private void ParseMetadata(string metadata)
        {
            try
            {
                // Parse title
                int titleIndex = metadata.IndexOf("xesam:title");
                if (titleIndex > 0)
                {
                    string titleLine = metadata.Substring(titleIndex).Split('\n')[0];
                    _currentTrack.Title = ExtractStringValue(titleLine);
                }
                
                // Parse artist
                int artistIndex = metadata.IndexOf("xesam:artist");
                if (artistIndex > 0)
                {
                    string artistSection = metadata.Substring(artistIndex);
                    int arrayStartIndex = artistSection.IndexOf("array [");
                    if (arrayStartIndex > 0)
                    {
                        string artistLine = artistSection.Substring(arrayStartIndex).Split('\n')[1];
                        _currentTrack.Artist = ExtractStringValue(artistLine);
                    }
                }
                
                // Parse album
                int albumIndex = metadata.IndexOf("xesam:album");
                if (albumIndex > 0)
                {
                    string albumLine = metadata.Substring(albumIndex).Split('\n')[0];
                    _currentTrack.Album = ExtractStringValue(albumLine);
                }
                
                // Parse duration
                int durationIndex = metadata.IndexOf("mpris:length");
                if (durationIndex > 0)
                {
                    string durationLine = metadata.Substring(durationIndex).Split('\n')[0];
                    int valueStartIndex = durationLine.IndexOf("int64") + 5;
                    string durationStr = durationLine.Substring(valueStartIndex).Trim();
                    if (long.TryParse(durationStr, out long duration))
                    {
                        _currentTrack.Duration = duration / 1000000f; // Convert microseconds to seconds
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error parsing metadata: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts a string value from a D-Bus output line.
        /// </summary>
        private string ExtractStringValue(string line)
        {
            try
            {
                // Find the string value part
                int valueStartIndex = line.IndexOf("string") + 6;
                if (valueStartIndex < 6) // If "string" wasn't found
                {
                    return "Unknown";
                }
                
                string valueWithQuotes = line.Substring(valueStartIndex).Trim();
                
                // Remove quotes if present
                if (valueWithQuotes.StartsWith("\"") && valueWithQuotes.EndsWith("\""))
                {
                    valueWithQuotes = valueWithQuotes.Substring(1, valueWithQuotes.Length - 2);
                }
                
                // Don't remove colons as they might be part of the actual title
                // Only remove them if they're at the beginning of the string
                if (valueWithQuotes.StartsWith(":"))
                {
                    valueWithQuotes = valueWithQuotes.Substring(1);
                }
                
                // If empty after processing, return Unknown
                if (string.IsNullOrWhiteSpace(valueWithQuotes))
                {
                    return "Unknown";
                }
                
                return valueWithQuotes;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting string value: {ex.Message}");
                return "Unknown";
            }
        }

        /// <summary>
        /// Executes a shell command.
        /// </summary>
        private string ExecuteCommand(string command)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing command: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
