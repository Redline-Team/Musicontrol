using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redline.Musicontrol.Runtime
{
    /// <summary>
    /// Factory for creating music player instances based on the current operating system.
    /// </summary>
    public static class MusicPlayerFactory
    {
        private static Dictionary<string, Type> _registeredPlayers = new Dictionary<string, Type>();
        private static List<IMusicPlayer> _availablePlayers = new List<IMusicPlayer>();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the factory and discovers available music players.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            _availablePlayers.Clear();
            
            // Register platform-specific players
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                    // Windows players will be registered by their implementations
                    break;
                case RuntimePlatform.OSXEditor:
                    // macOS players will be registered by their implementations
                    break;
                case RuntimePlatform.LinuxEditor:
                    // Linux players will be registered by their implementations
                    break;
                default:
                    Debug.LogWarning("Musicontrol: Unsupported platform for music player control.");
                    break;
            }

            _initialized = true;
        }

        /// <summary>
        /// Registers a music player type with the factory.
        /// </summary>
        /// <typeparam name="T">The type of music player to register.</typeparam>
        /// <param name="playerName">The name of the music player.</param>
        public static void RegisterPlayer<T>(string playerName) where T : IMusicPlayer
        {
            if (!_registeredPlayers.ContainsKey(playerName))
            {
                _registeredPlayers.Add(playerName, typeof(T));
            }
        }

        /// <summary>
        /// Gets all available music players.
        /// </summary>
        /// <param name="forceRefresh">If true, forces a refresh of the available players list.</param>
        /// <returns>A list of available music players.</returns>
        public static List<IMusicPlayer> GetAvailablePlayers(bool forceRefresh = false)
        {
            if (!_initialized)
                Initialize();

            if (_availablePlayers.Count == 0 || forceRefresh)
            {
                // Clear the list if we're refreshing
                if (forceRefresh)
                    _availablePlayers.Clear();
                
                Debug.Log("Musicontrol: Detecting available music players...");
                foreach (var entry in _registeredPlayers)
                {
                    try
                    {
                        string playerName = entry.Key;
                        Type playerType = entry.Value;
                        
                        Debug.Log($"Musicontrol: Checking if {playerName} is running...");
                        var player = Activator.CreateInstance(playerType) as IMusicPlayer;
                        
                        if (player != null)
                        {
                            bool isRunning = player.IsRunning;
                            Debug.Log($"Musicontrol: {playerName} is {(isRunning ? "running" : "not running")}.");
                            
                            if (isRunning)
                            {
                                _availablePlayers.Add(player);
                                Debug.Log($"Musicontrol: Added {playerName} to available players.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Musicontrol: Failed to create instance of {entry.Key}: {ex.Message}");
                    }
                }
                
                Debug.Log($"Musicontrol: Found {_availablePlayers.Count} available music players.");
            }

            return _availablePlayers;
        }

        /// <summary>
        /// Creates a new instance of a music player by name.
        /// </summary>
        /// <param name="playerName">The name of the music player to create.</param>
        /// <returns>A new instance of the specified music player, or null if not found.</returns>
        public static IMusicPlayer CreatePlayer(string playerName)
        {
            if (!_initialized)
                Initialize();

            if (_registeredPlayers.TryGetValue(playerName, out Type playerType))
            {
                try
                {
                    return Activator.CreateInstance(playerType) as IMusicPlayer;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Musicontrol: Failed to create instance of {playerName}: {ex.Message}");
                }
            }

            return null;
        }
    }
}
