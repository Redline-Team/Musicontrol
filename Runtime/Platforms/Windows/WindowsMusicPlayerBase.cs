using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Windows
{
    /// <summary>
    /// Base class for Windows music players.
    /// </summary>
    public abstract class WindowsMusicPlayerBase : IMusicPlayer
    {
        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        public abstract string PlayerName { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// Gets the current track information.
        /// </summary>
        public abstract TrackInfo CurrentTrack { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently playing.
        /// </summary>
        public abstract bool IsPlaying { get; }

        /// <summary>
        /// Play the current track.
        /// </summary>
        public abstract void Play();

        /// <summary>
        /// Pause the current track.
        /// </summary>
        public abstract void Pause();

        /// <summary>
        /// Play the next track.
        /// </summary>
        public abstract void Next();

        /// <summary>
        /// Play the previous track.
        /// </summary>
        public abstract void Previous();

        /// <summary>
        /// Set the volume level.
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0).</param>
        public abstract void SetVolume(float volume);

        /// <summary>
        /// Get the current volume level.
        /// </summary>
        /// <returns>Volume level (0.0 to 1.0).</returns>
        public abstract float GetVolume();

        /// <summary>
        /// Registers the Windows music players with the factory.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterWindowsPlayers()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                MusicPlayerFactory.RegisterPlayer<SpotifyWindowsPlayer>("Spotify");
                MusicPlayerFactory.RegisterPlayer<ITunesWindowsPlayer>("iTunes");
                MusicPlayerFactory.RegisterPlayer<WindowsMediaPlayer>("Windows Media Player");
            }
        }

        /// <summary>
        /// Sends a key combination to the active window.
        /// </summary>
        protected static void SendKeyboardShortcut(VirtualKeyCode key, bool ctrl = false, bool alt = false, bool shift = false)
        {
            try
            {
                if (ctrl) NativeMethods.keybd_event((byte)VirtualKeyCode.CONTROL, 0, 0, 0);
                if (alt) NativeMethods.keybd_event((byte)VirtualKeyCode.MENU, 0, 0, 0);
                if (shift) NativeMethods.keybd_event((byte)VirtualKeyCode.SHIFT, 0, 0, 0);

                NativeMethods.keybd_event((byte)key, 0, 0, 0);
                NativeMethods.keybd_event((byte)key, 0, NativeMethods.KEYEVENTF_KEYUP, 0);

                if (shift) NativeMethods.keybd_event((byte)VirtualKeyCode.SHIFT, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                if (alt) NativeMethods.keybd_event((byte)VirtualKeyCode.MENU, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
                if (ctrl) NativeMethods.keybd_event((byte)VirtualKeyCode.CONTROL, 0, NativeMethods.KEYEVENTF_KEYUP, 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending keyboard shortcut: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds a process by name.
        /// </summary>
        protected static IntPtr FindProcess(string processName)
        {
            IntPtr hWnd = IntPtr.Zero;
            NativeMethods.EnumWindows((hwnd, lParam) =>
            {
                int length = NativeMethods.GetWindowTextLength(hwnd);
                if (length > 0)
                {
                    System.Text.StringBuilder builder = new System.Text.StringBuilder(length + 1);
                    NativeMethods.GetWindowText(hwnd, builder, builder.Capacity);
                    if (builder.ToString().Contains(processName) && NativeMethods.IsWindowVisible(hwnd))
                    {
                        hWnd = hwnd;
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
            return hWnd;
        }
    }

    /// <summary>
    /// Native methods for Windows API calls.
    /// </summary>
    internal static class NativeMethods
    {
        public const int KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    /// <summary>
    /// Virtual key codes for keyboard input.
    /// </summary>
    public enum VirtualKeyCode : byte
    {
        CONTROL = 0x11,
        MENU = 0x12, // ALT key
        SHIFT = 0x10,
        SPACE = 0x20,
        LEFT = 0x25,
        UP = 0x26,
        RIGHT = 0x27,
        DOWN = 0x28,
        MEDIA_NEXT_TRACK = 0xB0,
        MEDIA_PREV_TRACK = 0xB1,
        MEDIA_STOP = 0xB2,
        MEDIA_PLAY_PAUSE = 0xB3,
        VOLUME_MUTE = 0xAD,
        VOLUME_DOWN = 0xAE,
        VOLUME_UP = 0xAF
    }
}
