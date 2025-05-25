using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Redline.Musicontrol.Runtime;
using Redline.Musicontrol.Runtime.Platforms.Linux;

namespace Redline.Musicontrol.Editor
{
    /// <summary>
    /// Editor window for controlling music players from within the Unity editor.
    /// </summary>
    public class MusicontrolWindow : EditorWindow
    {
        private List<IMusicPlayer> _availablePlayers = new List<IMusicPlayer>();
        private IMusicPlayer _selectedPlayer;
        private int _selectedPlayerIndex = -1;
        private Vector2 _scrollPosition;
        private float _volumeSliderValue = 0.5f;
        private bool _isInitialized = false;
        private Texture2D _albumArtTexture;
        private GUIStyle _headerStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _playerButtonStyle;
        private GUIStyle _controlButtonStyle;
        private GUIStyle _noPlayerStyle;
        private GUIContent _playButtonContent;
        private GUIContent _pauseButtonContent;
        private GUIContent _nextButtonContent;
        private GUIContent _prevButtonContent;
        private GUIContent _refreshButtonContent;
        private bool _isRefreshing = false;
        private double _lastRefreshTime = 0;
        private const double REFRESH_INTERVAL = 1.0; // Refresh every second
        private Color _originalBackgroundColor;
        
        // Manual connection options
        private bool _showManualConnectionOptions = false;
        private string[] _availablePlayerTypes = new string[] { "Spotify", "VLC", "Rhythmbox" };
        private int _selectedPlayerTypeIndex = 0;

        /// <summary>
        /// Opens the Musicontrol window.
        /// </summary>
        [MenuItem("Redline/Modules/Musicontrol/Music Player Control", false, 100)]
        public static void ShowWindow()
        {
            MusicontrolWindow window = GetWindow<MusicontrolWindow>("Music Player Control");
            window.minSize = new Vector2(350, 450);
            window.Show();
        }

        private void OnEnable()
        {
            _isInitialized = false;
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // Refresh player status periodically
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                if (_selectedPlayer != null && !_isRefreshing)
                {
                    Repaint();
                }
            }
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            // Initialize the factory
            MusicPlayerFactory.Initialize();
            
            // Get available players
            _availablePlayers = MusicPlayerFactory.GetAvailablePlayers();
            
            // Create styles
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };
            
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true
            };
            
            _infoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true
            };
            
            _playerButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 30,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            
            _controlButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 40,
                fixedHeight = 40
            };
            
            _noPlayerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            
            // Create button contents
            _playButtonContent = EditorGUIUtility.IconContent("d_PlayButton");
            _pauseButtonContent = EditorGUIUtility.IconContent("d_PauseButton");
            _nextButtonContent = EditorGUIUtility.IconContent("d_Animation.NextKey");
            _prevButtonContent = EditorGUIUtility.IconContent("d_Animation.PrevKey");
            _refreshButtonContent = EditorGUIUtility.IconContent("d_Refresh");
            
            _originalBackgroundColor = GUI.backgroundColor;
            _isInitialized = true;
        }

        private void OnGUI()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            DrawPlayerSelection();
            
            EditorGUILayout.Space(10);
            
            if (_selectedPlayer != null)
            {
                DrawPlayerControls();
                
                // Track info display removed for now
            }
            else
            {
                EditorGUILayout.HelpBox("No music player selected or available. Please select a player from the dropdown above.", MessageType.Info);
            }
            
            EditorGUILayout.Space(10);
            
            DrawManualConnectionOptions();
            
            EditorGUILayout.EndScrollView();
            
            // Repaint the window to update the UI
            Repaint();
        }

        private void DrawPlayerSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Player Selection", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Display a dropdown to select a music player
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Player:", GUILayout.Width(100));
            
            string[] playerNames = _availablePlayers.Select(p => p.PlayerName).ToArray();
            int newSelectedIndex = EditorGUILayout.Popup(_selectedPlayerIndex, playerNames);
            
            if (newSelectedIndex != _selectedPlayerIndex)
            {
                _selectedPlayerIndex = newSelectedIndex;
                if (_selectedPlayerIndex >= 0 && _selectedPlayerIndex < _availablePlayers.Count)
                {
                    _selectedPlayer = _availablePlayers[_selectedPlayerIndex];
                    _volumeSliderValue = _selectedPlayer.GetVolume();
                }
                else
                {
                    _selectedPlayer = null;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Refresh and Launch buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Available Players"))
            {
                RefreshPlayers();
            }
            
            // Only show Launch button if a player is selected
            if (_selectedPlayer != null)
            {
                if (GUILayout.Button("Launch Selected Player"))
                {
                    LaunchMusicPlayer(_selectedPlayer.PlayerName);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPlayerControls()
        {
            if (_selectedPlayer == null)
                return;
                
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Playback controls
            EditorGUILayout.BeginHorizontal();
            
            // Previous button
            if (GUILayout.Button(_prevButtonContent, _controlButtonStyle))
            {
                _selectedPlayer.Previous();
            }
            
            // Play/Pause button
            GUIContent playPauseContent = _selectedPlayer.IsPlaying ? _pauseButtonContent : _playButtonContent;
            if (GUILayout.Button(playPauseContent, _controlButtonStyle))
            {
                if (_selectedPlayer.IsPlaying)
                {
                    _selectedPlayer.Pause();
                }
                else
                {
                    _selectedPlayer.Play();
                }
            }
            
            // Next button
            if (GUILayout.Button(_nextButtonContent, _controlButtonStyle))
            {
                _selectedPlayer.Next();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Track info update button removed
            
            // Volume slider
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Volume:", GUILayout.Width(60));
            
            EditorGUI.BeginChangeCheck();
            _volumeSliderValue = EditorGUILayout.Slider(_volumeSliderValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                _selectedPlayer.SetVolume(_volumeSliderValue);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        // Track information display has been removed for simplicity

        private void RefreshPlayers()
        {
            _isRefreshing = true;
            
            // Log that we're refreshing players
            Debug.Log("Musicontrol: Refreshing available music players...");
            
            // Get available players with force refresh
            _availablePlayers = MusicPlayerFactory.GetAvailablePlayers(forceRefresh: true);
            
            // Try to keep the same player selected if possible
            if (_selectedPlayer != null)
            {
                string currentPlayerName = _selectedPlayer.PlayerName;
                _selectedPlayerIndex = -1;
                
                for (int i = 0; i < _availablePlayers.Count; i++)
                {
                    if (_availablePlayers[i].PlayerName == currentPlayerName)
                    {
                        _selectedPlayerIndex = i;
                        _selectedPlayer = _availablePlayers[i];
                        break;
                    }
                }
                
                if (_selectedPlayerIndex == -1)
                {
                    _selectedPlayer = null;
                }
            }
            
            _isRefreshing = false;
            
            // Force repaint to update the UI
            Repaint();
            
            Debug.Log($"Musicontrol: Refresh complete. Found {_availablePlayers.Count} available music players.");
        }

        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int remainingSeconds = Mathf.FloorToInt(seconds % 60);
            return $"{minutes}:{remainingSeconds:00}";
        }
        
        /// <summary>
        /// Draws the manual connection options UI.
        /// </summary>
        private void DrawManualConnectionOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            _showManualConnectionOptions = EditorGUILayout.Foldout(_showManualConnectionOptions, "Manual Connection Options (for Linux/Wayland)", true);
            
            if (_showManualConnectionOptions)
            {
                EditorGUILayout.HelpBox("If automatic detection doesn't work (common on Linux with Wayland), you can manually connect to a running music player.", MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Player Type:", GUILayout.Width(100));
                _selectedPlayerTypeIndex = EditorGUILayout.Popup(_selectedPlayerTypeIndex, _availablePlayerTypes);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Launch Player", GUILayout.Height(30)))
                {
                    LaunchMusicPlayer(_availablePlayerTypes[_selectedPlayerTypeIndex]);
                }
                
                if (GUILayout.Button("Connect Manually", GUILayout.Height(30)))
                {
                    ConnectManually(_availablePlayerTypes[_selectedPlayerTypeIndex]);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.HelpBox("Launching the player from Unity may improve detection, especially on Wayland.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Connects manually to a music player.
        /// </summary>
        /// <param name="playerType">The type of music player to connect to.</param>
        private void ConnectManually(string playerType)
        {
            try
            {
                Debug.Log($"Manually connecting to {playerType}...");
                
                // Create a manual music player
                var manualPlayer = new ManualMusicPlayer(playerType);
                
                // Add it to the available players list
                _availablePlayers.Add(manualPlayer);
                
                // Select the new player
                _selectedPlayerIndex = _availablePlayers.Count - 1;
                _selectedPlayer = manualPlayer;
                _volumeSliderValue = _selectedPlayer.GetVolume();
                
                Debug.Log($"Successfully connected to {playerType} manually.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error connecting to {playerType} manually: {ex.Message}");
                EditorUtility.DisplayDialog("Connection Error", $"Failed to connect to {playerType}. Make sure it is running and try again.", "OK");
            }
        }
        
        /// <summary>
        /// Launches a music player application.
        /// </summary>
        /// <param name="playerType">The type of music player to launch.</param>
        private void LaunchMusicPlayer(string playerType)
        {
            try
            {
                Debug.Log($"Launching {playerType}...");
                
                bool success = MusicPlayerLauncher.LaunchPlayer(playerType);
                
                if (success)
                {
                    Debug.Log($"Successfully launched {playerType}.");
                    EditorUtility.DisplayDialog("Player Launched", $"{playerType} has been launched. Wait a moment for it to start, then click 'Connect Manually' or 'Refresh Available Players'.", "OK");
                }
                else
                {
                    Debug.LogError($"Failed to launch {playerType}.");
                    EditorUtility.DisplayDialog("Launch Error", $"Failed to launch {playerType}. Please start it manually and then click 'Connect Manually'.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error launching {playerType}: {ex.Message}");
                EditorUtility.DisplayDialog("Launch Error", $"Error launching {playerType}: {ex.Message}", "OK");
            }
        }
    }
}
