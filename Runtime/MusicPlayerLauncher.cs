using System;
using System.Diagnostics;
using UnityEngine;

namespace Redline.Musicontrol.Runtime
{
    /// <summary>
    /// Provides methods to launch music players on different platforms.
    /// </summary>
    public static class MusicPlayerLauncher
    {
        /// <summary>
        /// Launches a music player application.
        /// </summary>
        /// <param name="playerType">The type of music player to launch.</param>
        /// <returns>True if the player was launched successfully; otherwise, false.</returns>
        public static bool LaunchPlayer(string playerType)
        {
            try
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                        return LaunchPlayerWindows(playerType);
                    case RuntimePlatform.OSXEditor:
                        return LaunchPlayerMacOS(playerType);
                    case RuntimePlatform.LinuxEditor:
                        return LaunchPlayerLinux(playerType);
                    default:
                        UnityEngine.Debug.LogWarning($"Musicontrol: Unsupported platform for launching music players.");
                        return false;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Musicontrol: Error launching {playerType}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Launches a music player application on Windows.
        /// </summary>
        /// <param name="playerType">The type of music player to launch.</param>
        /// <returns>True if the player was launched successfully; otherwise, false.</returns>
        private static bool LaunchPlayerWindows(string playerType)
        {
            string executablePath = "";
            
            switch (playerType.ToLower())
            {
                case "spotify":
                    // Try to find Spotify in common installation locations
                    string[] spotifyPaths = new string[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Spotify\\Spotify.exe",
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\Spotify\\Spotify.exe",
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\Spotify\\Spotify.exe"
                    };
                    
                    foreach (string path in spotifyPaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            executablePath = path;
                            break;
                        }
                    }
                    
                    // If not found, try to launch via URI
                    if (string.IsNullOrEmpty(executablePath))
                    {
                        Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
                        return true;
                    }
                    break;
                    
                case "itunes":
                case "apple music":
                    // Try to find iTunes in common installation locations
                    string[] itunesPaths = new string[]
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\iTunes\\iTunes.exe",
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\iTunes\\iTunes.exe"
                    };
                    
                    foreach (string path in itunesPaths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            executablePath = path;
                            break;
                        }
                    }
                    break;
                    
                case "windows media player":
                    executablePath = Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\wmplayer.exe";
                    break;
                    
                default:
                    UnityEngine.Debug.LogWarning($"Musicontrol: Unknown player type '{playerType}' for Windows.");
                    return false;
            }
            
            if (!string.IsNullOrEmpty(executablePath))
            {
                Process.Start(executablePath);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Launches a music player application on macOS.
        /// </summary>
        /// <param name="playerType">The type of music player to launch.</param>
        /// <returns>True if the player was launched successfully; otherwise, false.</returns>
        private static bool LaunchPlayerMacOS(string playerType)
        {
            string appName = "";
            
            switch (playerType.ToLower())
            {
                case "spotify":
                    appName = "Spotify";
                    break;
                case "apple music":
                    appName = "Music";
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Musicontrol: Unknown player type '{playerType}' for macOS.");
                    return false;
            }
            
            // Use AppleScript to launch the application
            string script = $"tell application \"{appName}\" to activate";
            
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/osascript",
                Arguments = $"-e \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            Process.Start(psi);
            return true;
        }

        /// <summary>
        /// Launches a music player application on Linux.
        /// </summary>
        /// <param name="playerType">The type of music player to launch.</param>
        /// <returns>True if the player was launched successfully; otherwise, false.</returns>
        private static bool LaunchPlayerLinux(string playerType)
        {
            string command = "";
            
            switch (playerType.ToLower())
            {
                case "spotify":
                    // Try different ways to launch Spotify on Linux
                    string[] spotifyCommands = new string[]
                    {
                        "spotify",                // Standard command
                        "flatpak run com.spotify.Client", // Flatpak
                        "snap run spotify",       // Snap
                        "gtk-launch spotify"      // Desktop file
                    };
                    
                    foreach (string cmd in spotifyCommands)
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = "/bin/bash",
                                Arguments = $"-c \"{cmd} &\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            
                            Process.Start(psi);
                            return true;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning($"Musicontrol: Failed to launch Spotify with command '{cmd}': {ex.Message}");
                            // Continue trying other commands
                        }
                    }
                    
                    UnityEngine.Debug.LogError("Musicontrol: All attempts to launch Spotify failed.");
                    return false;
                    
                case "vlc":
                    command = "vlc";
                    break;
                    
                case "rhythmbox":
                    command = "rhythmbox";
                    break;
                    
                default:
                    // Try a generic approach for unknown players
                    command = playerType.ToLower();
                    break;
            }
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command} &\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Musicontrol: Failed to launch {playerType} with command '{command}': {ex.Message}");
                return false;
            }
        }
    }
}
