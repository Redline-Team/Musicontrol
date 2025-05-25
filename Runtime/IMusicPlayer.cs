using System;
using UnityEngine;

namespace Redline.Musicontrol.Runtime
{
    /// <summary>
    /// Interface for music player control operations.
    /// </summary>
    public interface IMusicPlayer
    {
        /// <summary>
        /// Gets the name of the music player.
        /// </summary>
        string PlayerName { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Gets the current track information.
        /// </summary>
        TrackInfo CurrentTrack { get; }

        /// <summary>
        /// Gets a value indicating whether the player is currently playing.
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// Play the current track.
        /// </summary>
        void Play();

        /// <summary>
        /// Pause the current track.
        /// </summary>
        void Pause();

        /// <summary>
        /// Play the next track.
        /// </summary>
        void Next();

        /// <summary>
        /// Play the previous track.
        /// </summary>
        void Previous();

        /// <summary>
        /// Set the volume level.
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0).</param>
        void SetVolume(float volume);

        /// <summary>
        /// Get the current volume level.
        /// </summary>
        /// <returns>Volume level (0.0 to 1.0).</returns>
        float GetVolume();
    }

    /// <summary>
    /// Represents information about a music track.
    /// </summary>
    [Serializable]
    public class TrackInfo
    {
        private string _title = "Unknown";
        private string _artist = "Unknown";
        private string _album = "Unknown";
        
        /// <summary>
        /// Gets or sets the title of the track.
        /// </summary>
        public string Title 
        { 
            get => _title; 
            set => _title = string.IsNullOrEmpty(value) ? "Unable To Retrieve" : value; 
        }

        /// <summary>
        /// Gets or sets the artist of the track.
        /// </summary>
        public string Artist 
        { 
            get => _artist; 
            set => _artist = string.IsNullOrEmpty(value) ? "Unknown Artist" : value; 
        }

        /// <summary>
        /// Gets or sets the album of the track.
        /// </summary>
        public string Album 
        { 
            get => _album; 
            set => _album = string.IsNullOrEmpty(value) ? "Unknown Album" : value; 
        }

        /// <summary>
        /// Gets or sets the duration of the track in seconds.
        /// </summary>
        public float Duration { get; set; }

        /// <summary>
        /// Gets or sets the current position in the track in seconds.
        /// </summary>
        public float Position { get; set; }

        /// <summary>
        /// Gets or sets the album art texture.
        /// </summary>
        public Texture2D AlbumArt { get; set; }
        
        /// <summary>
        /// Gets a value indicating whether track information is available.
        /// </summary>
        public bool HasTrackInfo => _title != "Unknown" && _title != "Unable To Retrieve";

        /// <summary>
        /// Creates a new instance of the TrackInfo class.
        /// </summary>
        public TrackInfo()
        {
            Title = "Unknown";
            Artist = "Unknown";
            Album = "Unknown";
            Duration = 0;
            Position = 0;
            AlbumArt = null;
        }
        
        /// <summary>
        /// Resets the track information to unknown values.
        /// </summary>
        public void Reset()
        {
            Title = "Unable To Retrieve";
            Artist = "Unknown Artist";
            Album = "Unknown Album";
            Duration = 0;
            Position = 0;
            AlbumArt = null;
        }
    }
}
