using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>The audio engine and the play queue.</summary>
    public interface IPlaybackService : INotifyPropertyChanged, IDisposable
    {
        ObservableCollection<Song> Queue { get; }
        Song? CurrentSong { get; }
        int CurrentIndex { get; }

        bool IsShuffleOn { get; set; }
        RepeatMode RepeatMode { get; set; }
        PlaybackState PlaybackState { get; }

        TimeSpan CurrentPosition { get; set; }
        TimeSpan TotalDuration { get; }
        float Volume { get; set; }

        /// <summary>Raised whenever a track starts, including replays of the same track.</summary>
        event EventHandler<Song>? TrackStarted;

        event EventHandler? CurrentSongChanged;
        event EventHandler? PlaybackStateChanged;
        event EventHandler? PositionChanged;
        event EventHandler? QueueChanged;

        void SetQueue(IEnumerable<Song> songs, int startIndex = 0);
        void PlaySong(Song song);
        void PlaySongFromCollection(Song song, IEnumerable<Song> collection);
        void PlaySongAtIndex(int index);

        /// <summary>
        /// Loads a queue entry and parks the playhead at <paramref name="position"/> without
        /// starting playback, so a restored session opens exactly where it was left.
        /// </summary>
        void PrepareAt(int index, TimeSpan position);

        void AddToQueue(Song song);
        void RemoveFromQueue(Song song);

        void Play();
        void Pause();
        void Stop();
        void Seek(TimeSpan position);
        void PlayNext();
        void PlayPrevious();

        /// <summary>
        /// Replaces the audio source without changing the current song, keeping the playhead
        /// where it was. Used to swap between the original track and its instrumental stem.
        /// </summary>
        void SwapSource(string filePath);
    }
}
