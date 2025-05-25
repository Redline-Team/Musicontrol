using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.Linux
{
    /// <summary>
    /// Base class for Linux music players.
    /// </summary>
    public abstract class LinuxMusicPlayerBase : IMusicPlayer
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
        /// Registers the Linux music players with the factory.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterLinuxPlayers()
        {
            if (Application.platform == RuntimePlatform.LinuxEditor)
            {
                Debug.Log("Registering Linux music players...");
                
                // Check if we're running on Wayland or X11
                string displayServer = "Unknown";
                try
                {
                    string wayland = System.Environment.GetEnvironmentVariable("WAYLAND_DISPLAY");
                    string x11 = System.Environment.GetEnvironmentVariable("DISPLAY");
                    
                    if (!string.IsNullOrEmpty(wayland))
                    {
                        displayServer = "Wayland";
                    }
                    else if (!string.IsNullOrEmpty(x11))
                    {
                        displayServer = "X11";
                    }
                    
                    Debug.Log($"Detected display server: {displayServer}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error detecting display server: {ex.Message}");
                }
                
                MusicPlayerFactory.RegisterPlayer<SpotifyLinuxPlayer>("Spotify");
                MusicPlayerFactory.RegisterPlayer<RhythmboxPlayer>("Rhythmbox");
                MusicPlayerFactory.RegisterPlayer<VlcPlayer>("VLC");
                
                Debug.Log("Linux music players registered.");
            }
        }

        /// <summary>
        /// Executes a shell command with improved error handling.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <returns>The result of the command execution.</returns>
        protected string ExecuteShellCommand(string command)
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
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"Shell command produced error output: {error}");
                }
                
                return output.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing shell command: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if a process is running using multiple detection methods to support both X11 and Wayland.
        /// </summary>
        /// <param name="processName">The name of the process to check.</param>
        /// <returns>True if the process is running; otherwise, false.</returns>
        protected bool IsProcessRunning(string processName)
        {
            // Method 1: Use pgrep (works on both X11 and Wayland)
            string pgrepResult = ExecuteShellCommand($"pgrep -x {processName} || echo ''");
            if (!string.IsNullOrEmpty(pgrepResult))
            {
                Debug.Log($"Process '{processName}' detected using pgrep.");
                return true;
            }
            
            // Method 2: Use ps (works on both X11 and Wayland)
            string psResult = ExecuteShellCommand($"ps aux | grep -v grep | grep {processName} || echo ''");
            if (!string.IsNullOrEmpty(psResult) && !psResult.Contains("grep"))
            {
                Debug.Log($"Process '{processName}' detected using ps.");
                return true;
            }
            
            // Method 3: Check if the application is in the list of running applications
            string wmctrlResult = ExecuteShellCommand($"wmctrl -l | grep -i {processName} || echo ''");
            if (!string.IsNullOrEmpty(wmctrlResult))
            {
                Debug.Log($"Process '{processName}' detected using wmctrl (X11).");
                return true;
            }
            
            Debug.Log($"Process '{processName}' not detected using any method.");
            return false;
        }

        /// <summary>
        /// Sends a D-Bus command with improved error handling for both X11 and Wayland environments.
        /// </summary>
        /// <param name="service">The D-Bus service name.</param>
        /// <param name="path">The D-Bus object path.</param>
        /// <param name="dbusInterfaceName">The D-Bus interface.</param>
        /// <param name="method">The method to call.</param>
        /// <param name="args">Optional arguments for the method.</param>
        /// <returns>The result of the D-Bus command.</returns>
        protected string SendDBusCommand(string service, string path, string dbusInterfaceName, string method, string args = "")
        {
            try
            {
                // Build the D-Bus command
                string command = $"dbus-send --print-reply --dest={service} {path} {dbusInterfaceName}.{method}";
                
                // Add arguments if provided
                if (!string.IsNullOrEmpty(args))
                {
                    command += " " + args;
                }
                
                Debug.Log($"Executing D-Bus command: {command}");
                
                // Execute the command and return the result
                string result = ExecuteShellCommand(command);
                
                // Check for common D-Bus errors
                if (result.Contains("Error") || result.Contains("Failed") || result.Contains("No such"))
                {
                    Debug.LogWarning($"D-Bus command returned an error: {result}");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing D-Bus command: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
