using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Linux
{
    /// <summary>
    /// Implementation of Spotify music player control for Linux.
    /// </summary>
    public class SpotifyLinuxPlayer : LinuxMusicPlayerBase
    {
        private const string DBUS_SERVICE = "org.mpris.MediaPlayer2.spotify";
        private const string DBUS_PATH = "/org/mpris/MediaPlayer2";
        private const string DBUS_PLAYER_INTERFACE = "org.mpris.MediaPlayer2.Player";
        private const string DBUS_PROPERTIES_INTERFACE = "org.freedesktop.DBus.Properties";

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
        public override bool IsRunning
        {
            get
            {
                Debug.Log("Checking if Spotify is running...");
                
                // Method 1: Check if the process is running (works on both X11 and Wayland)
                bool processRunning = IsProcessRunning("spotify");
                Debug.Log($"Spotify process running: {processRunning}");
                
                if (!processRunning)
                {
                    // Also check for the snap version of Spotify
                    processRunning = IsProcessRunning("spotify.spotify");
                    Debug.Log($"Spotify snap process running: {processRunning}");
                    
                    if (!processRunning)
                        return false;
                }
                
                // Method 2: Check if the D-Bus service is available (works on both X11 and Wayland)
                try
                {
                    // Use playerctl to check if Spotify is available
                    string playerctlResult = ExecuteShellCommand("playerctl -l | grep spotify || echo ''");
                    bool playerctlDetected = !string.IsNullOrEmpty(playerctlResult);
                    Debug.Log($"Spotify detected via playerctl: {playerctlDetected}");
                    
                    if (playerctlDetected)
                        return true;
                    
                    // Check D-Bus directly
                    string dbusResult = ExecuteShellCommand("dbus-send --print-reply --dest=org.freedesktop.DBus --type=method_call /org/freedesktop/DBus org.freedesktop.DBus.ListNames | grep org.mpris.MediaPlayer2.spotify || echo ''");
                    bool dbusDetected = !string.IsNullOrEmpty(dbusResult);
                    Debug.Log($"Spotify detected via D-Bus: {dbusDetected}");
                    
                    if (dbusDetected)
                        return true;
                    
                    // Method 3: Try to directly communicate with Spotify
                    try
                    {
                        string statusResult = ExecuteShellCommand("dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:'org.mpris.MediaPlayer2.Player' string:'PlaybackStatus' 2>/dev/null || echo ''");
                        bool canCommunicate = !string.IsNullOrEmpty(statusResult) && !statusResult.Contains("Error");
                        Debug.Log($"Can communicate with Spotify via D-Bus: {canCommunicate}");
                        
                        return canCommunicate;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error communicating with Spotify: {ex.Message}");
                    }
                    
                    // If all else fails, fall back to process detection
                    Debug.Log("Falling back to process detection for Spotify");
                    return processRunning;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error checking if Spotify is available: {ex.Message}");
                    return processRunning; // Fall back to just checking the process
                }
            }
        }

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
                    Debug.LogError($"Error checking if Spotify is playing: {ex.Message}");
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
                    Debug.LogError($"Error getting Spotify volume: {ex.Message}");
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
                // Use the exact format that works with Spotify
                string command = "dbus-send --print-reply --dest=org.mpris.MediaPlayer2.spotify /org/mpris/MediaPlayer2 org.freedesktop.DBus.Properties.Get string:org.mpris.MediaPlayer2.Player string:Metadata";
                string result = ExecuteShellCommand(command);
                
                // Log the raw D-Bus output for debugging
                Debug.Log($"Raw Spotify D-Bus Metadata output:\n{result}");
                
                if (!string.IsNullOrEmpty(result))
                {
                    // Based on the actual D-Bus output format, extract the track information
                    
                    // Parse title - Format: string "xesam:title" variant string "Phoenix"
                    int titleIndex = result.IndexOf("xesam:title");
                    if (titleIndex > 0)
                    {
                        // Find the variant string line
                        string titleSection = result.Substring(titleIndex);
                        int variantIndex = titleSection.IndexOf("variant");
                        if (variantIndex > 0)
                        {
                            string variantLine = titleSection.Substring(variantIndex);
                            int stringIndex = variantLine.IndexOf("string");
                            if (stringIndex > 0)
                            {
                                string titleValue = variantLine.Substring(stringIndex + 6).Trim();
                                // Remove quotes if present
                                if (titleValue.StartsWith("\"") && titleValue.EndsWith("\""))
                                {
                                    titleValue = titleValue.Substring(1, titleValue.Length - 2);
                                }
                                Debug.Log($"Extracted title: {titleValue}");
                                _currentTrack.Title = titleValue;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Could not find xesam:title in metadata");
                        _currentTrack.Title = "Unknown";
                    }
                    
                    // Parse artist - Format: string "xesam:artist" variant array [ string "Netrum" ]
                    int artistIndex = result.IndexOf("xesam:artist");
                    if (artistIndex > 0)
                    {
                        try
                        {
                            string artistSection = result.Substring(artistIndex);
                            int arrayStartIndex = artistSection.IndexOf("array [");
                            if (arrayStartIndex > 0)
                            {
                                string arrayContent = artistSection.Substring(arrayStartIndex + 7);
                                int stringIndex = arrayContent.IndexOf("string");
                                if (stringIndex >= 0)
                                {
                                    string artistValue = arrayContent.Substring(stringIndex + 6).Trim();
                                    // Find the first string value in the array
                                    if (artistValue.StartsWith("\"") && artistValue.Contains("\""))
                                    {
                                        artistValue = artistValue.Substring(1, artistValue.IndexOf('"', 1) - 1);
                                    }
                                    Debug.Log($"Extracted artist: {artistValue}");
                                    _currentTrack.Artist = artistValue;
                                }
                                else
                                {
                                    Debug.Log("Could not find string in artist array");
                                    _currentTrack.Artist = "Unknown Artist";
                                }
                            }
                            else
                            {
                                Debug.Log("Could not find array start in xesam:artist section");
                                _currentTrack.Artist = "Unknown Artist";
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error parsing artist: {ex.Message}");
                            _currentTrack.Artist = "Unknown Artist";
                        }
                    }
                    else
                    {
                        Debug.Log("Could not find xesam:artist in metadata");
                        _currentTrack.Artist = "Unknown Artist";
                    }
                    
                    // Parse album - Format: string "xesam:album" variant string "Phoenix"
                    int albumIndex = result.IndexOf("xesam:album");
                    if (albumIndex > 0)
                    {
                        // Find the variant string line
                        string albumSection = result.Substring(albumIndex);
                        int variantIndex = albumSection.IndexOf("variant");
                        if (variantIndex > 0)
                        {
                            string variantLine = albumSection.Substring(variantIndex);
                            int stringIndex = variantLine.IndexOf("string");
                            if (stringIndex > 0)
                            {
                                string albumValue = variantLine.Substring(stringIndex + 6).Trim();
                                // Remove quotes if present
                                if (albumValue.StartsWith("\"") && albumValue.EndsWith("\""))
                                {
                                    albumValue = albumValue.Substring(1, albumValue.Length - 2);
                                }
                                Debug.Log($"Extracted album: {albumValue}");
                                _currentTrack.Album = albumValue;
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Could not find xesam:album in metadata");
                        _currentTrack.Album = "Unknown Album";
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
                Debug.LogError($"Error updating Spotify track info: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Extracts a string value from a D-Bus output line.
        /// </summary>
        private string ExtractStringValue(string line)
        {
            try
            {
                Debug.Log($"Parsing line: {line}");
                
                // Find the string value part
                int valueStartIndex = line.IndexOf("string") + 6;
                if (valueStartIndex < 6) // If "string" wasn't found
                {
                    Debug.Log("No 'string' keyword found in line");
                    return "Unknown";
                }
                
                string valueWithQuotes = line.Substring(valueStartIndex).Trim();
                Debug.Log($"Extracted value with quotes: {valueWithQuotes}");
                
                // Remove quotes if present
                if (valueWithQuotes.StartsWith("\"") && valueWithQuotes.EndsWith("\""))
                {
                    valueWithQuotes = valueWithQuotes.Substring(1, valueWithQuotes.Length - 2);
                    Debug.Log($"Value after removing quotes: {valueWithQuotes}");
                }
                
                // Don't remove colons as they might be part of the actual title
                // Only remove them if they're at the beginning of the string
                if (valueWithQuotes.StartsWith(":"))
                {
                    valueWithQuotes = valueWithQuotes.Substring(1);
                    Debug.Log($"Value after removing leading colon: {valueWithQuotes}");
                }
                
                // If empty after processing, return Unknown
                if (string.IsNullOrWhiteSpace(valueWithQuotes))
                {
                    Debug.Log("Value is empty after processing");
                    return "Unknown";
                }
                
                Debug.Log($"Final extracted value: {valueWithQuotes}");
                return valueWithQuotes;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error extracting string value: {ex.Message}");
                return "Unknown";
            }
        }
    }
}
