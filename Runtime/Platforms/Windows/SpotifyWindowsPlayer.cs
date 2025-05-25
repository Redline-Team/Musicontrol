using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Windows
{
    /// <summary>
    /// Implementation of Spotify music player control for Windows.
    /// </summary>
    public class SpotifyWindowsPlayer : WindowsMusicPlayerBase
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
        public override bool IsRunning
        {
            get
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName("Spotify");
                    return processes.Length > 0;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Error checking if Spotify is running: {ex.Message}");
                    return false;
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
        public override bool IsPlaying => _isPlaying;

        /// <summary>
        /// Play the current track.
        /// </summary>
        public override void Play()
        {
            IntPtr hWnd = FindProcess("Spotify");
            if (hWnd != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(hWnd);
                SendKeyboardShortcut(VirtualKeyCode.SPACE);
                _isPlaying = true;
            }
        }

        /// <summary>
        /// Pause the current track.
        /// </summary>
        public override void Pause()
        {
            IntPtr hWnd = FindProcess("Spotify");
            if (hWnd != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(hWnd);
                SendKeyboardShortcut(VirtualKeyCode.SPACE);
                _isPlaying = false;
            }
        }

        /// <summary>
        /// Play the next track.
        /// </summary>
        public override void Next()
        {
            IntPtr hWnd = FindProcess("Spotify");
            if (hWnd != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(hWnd);
                SendKeyboardShortcut(VirtualKeyCode.RIGHT, ctrl: true);
            }
        }

        /// <summary>
        /// Play the previous track.
        /// </summary>
        public override void Previous()
        {
            IntPtr hWnd = FindProcess("Spotify");
            if (hWnd != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(hWnd);
                SendKeyboardShortcut(VirtualKeyCode.LEFT, ctrl: true);
            }
        }

        /// <summary>
        /// Set the volume level.
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0).</param>
        public override void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            // Spotify doesn't have a direct volume control via keyboard shortcuts
            // This would require additional implementation using Windows audio APIs
        }

        /// <summary>
        /// Get the current volume level.
        /// </summary>
        /// <returns>Volume level (0.0 to 1.0).</returns>
        public override float GetVolume()
        {
            return _volume;
        }

        /// <summary>
        /// Updates the current track information.
        /// </summary>
        private void UpdateTrackInfo()
        {
            IntPtr hWnd = FindProcess("Spotify");
            if (hWnd != IntPtr.Zero)
            {
                int length = NativeMethods.GetWindowTextLength(hWnd);
                if (length > 0)
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder(length + 1);
                    NativeMethods.GetWindowText(hWnd, builder, builder.Capacity);
                    string windowTitle = builder.ToString();

                    // Spotify window title format: "Song - Artist - Spotify"
                    if (windowTitle != "Spotify" && windowTitle.EndsWith("Spotify"))
                    {
                        string[] parts = windowTitle.Split(new string[] { " - " }, StringSplitOptions.None);
                        if (parts.Length >= 3)
                        {
                            _currentTrack.Title = parts[0];
                            _currentTrack.Artist = parts[1];
                            _currentTrack.Album = "Unknown"; // Spotify window title doesn't include album
                            _isPlaying = true;
                        }
                    }
                    else
                    {
                        _isPlaying = false;
                    }
                }
            }
        }
    }
}
