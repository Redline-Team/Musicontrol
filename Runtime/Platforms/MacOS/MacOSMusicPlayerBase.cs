using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime.Platforms.MacOS
{
    /// <summary>
    /// Base class for macOS music players.
    /// </summary>
    public abstract class MacOSMusicPlayerBase : IMusicPlayer
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
        /// Registers the macOS music players with the factory.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterMacOSPlayers()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                MusicPlayerFactory.RegisterPlayer<AppleMusicPlayer>("Apple Music");
                MusicPlayerFactory.RegisterPlayer<SpotifyMacOSPlayer>("Spotify");
            }
        }

        /// <summary>
        /// Executes an AppleScript command.
        /// </summary>
        /// <param name="script">The AppleScript to execute.</param>
        /// <returns>The result of the AppleScript execution.</returns>
        protected string ExecuteAppleScript(string script)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "osascript";
                process.StartInfo.Arguments = $"-e \"{script.Replace("\"", "\\\"")}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                return output.Trim();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing AppleScript: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Checks if an application is running.
        /// </summary>
        /// <param name="appName">The name of the application to check.</param>
        /// <returns>True if the application is running; otherwise, false.</returns>
        protected bool IsApplicationRunning(string appName)
        {
            string script = $"tell application \"System Events\" to (name of processes) contains \"{appName}\"";
            string result = ExecuteAppleScript(script);
            return result.ToLower() == "true";
        }
    }
}
