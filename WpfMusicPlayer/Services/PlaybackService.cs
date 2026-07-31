using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using NAudio.Wave;
using WpfMusicPlayer.Models;

// NAudio has its own PlaybackState; the app's model type is the one meant everywhere here
using PlaybackState = WpfMusicPlayer.Models.PlaybackState;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Owns the audio engine and the play queue. Knows nothing about playlists, statistics,
    /// stem separation or the UI.
    /// </summary>
    public class PlaybackService : IPlaybackService
    {
        private readonly DispatcherTimer _positionTimer;
        private readonly QueueNavigator _navigator = new();

        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioFileReader;
        private Song? _currentSong;
        private float _volume = 0.8f;

        private bool _suppressAutoAdvance;
        private bool _isSwitchingTrack;
        private bool _disposed;

        public PlaybackService()
        {
            _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _positionTimer.Tick += (_, _) =>
            {
                OnPropertyChanged(nameof(CurrentPosition));
                PositionChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        public ObservableCollection<Song> Queue { get; } = new();

        public Song? CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (_currentSong == value)
                    return;

                _currentSong = value;
                OnPropertyChanged();
                CurrentSongChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int CurrentIndex => _navigator.CurrentIndex;

        public bool IsShuffleOn
        {
            get => _navigator.IsShuffleOn;
            set
            {
                if (_navigator.IsShuffleOn == value)
                    return;

                _navigator.IsShuffleOn = value;
                OnPropertyChanged();
            }
        }

        public RepeatMode RepeatMode
        {
            get => _navigator.RepeatMode;
            set
            {
                if (_navigator.RepeatMode == value)
                    return;

                _navigator.RepeatMode = value;
                OnPropertyChanged();
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (_wavePlayer == null)
                    return PlaybackState.Stopped;

                return _wavePlayer.PlaybackState switch
                {
                    NAudio.Wave.PlaybackState.Playing => PlaybackState.Playing,
                    NAudio.Wave.PlaybackState.Paused => PlaybackState.Paused,
                    _ => PlaybackState.Stopped
                };
            }
        }

        public TimeSpan CurrentPosition
        {
            get => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
            set
            {
                if (_audioFileReader == null)
                    return;

                var clamped = TimeSpan.FromSeconds(
                    Math.Max(0, Math.Min(value.TotalSeconds, _audioFileReader.TotalTime.TotalSeconds)));

                _audioFileReader.CurrentTime = clamped;
            }
        }

        public TimeSpan TotalDuration => _audioFileReader?.TotalTime ?? TimeSpan.Zero;

        public float Volume
        {
            get => _volume;
            set
            {
                var clamped = Math.Max(0f, Math.Min(1f, value));
                if (Math.Abs(_volume - clamped) < 0.0001f)
                    return;

                _volume = clamped;

                if (_audioFileReader != null)
                    _audioFileReader.Volume = _volume;

                OnPropertyChanged();
            }
        }

        public event EventHandler<Song>? TrackStarted;
        public event EventHandler? CurrentSongChanged;
        public event EventHandler? PlaybackStateChanged;
        public event EventHandler? PositionChanged;
        public event EventHandler? QueueChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        #region Queue

        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
        {
            Queue.Clear();
            foreach (var song in songs)
                Queue.Add(song);

            _navigator.Reset(Queue.Count, startIndex);
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PlaySong(Song song)
        {
            var index = Queue.ToList().FindIndex(s => s.Equals(song));
            if (index >= 0)
            {
                PlaySongAtIndex(index);
                return;
            }

            Queue.Add(song);
            _navigator.ItemAdded();
            QueueChanged?.Invoke(this, EventArgs.Empty);
            PlaySongAtIndex(Queue.Count - 1);
        }

        public void PlaySongFromCollection(Song song, IEnumerable<Song> collection)
        {
            var songList = collection.ToList();
            var index = songList.FindIndex(s => s.Equals(song));
            if (index < 0)
                return;

            SetQueue(songList, index);
            PlaySongAtIndex(index);
        }

        public void AddToQueue(Song song)
        {
            Queue.Add(song);
            _navigator.ItemAdded();
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveFromQueue(Song song)
        {
            var index = Queue.ToList().FindIndex(s => s.Equals(song));
            if (index < 0)
                return;

            var wasCurrent = index == _navigator.CurrentIndex;

            Queue.RemoveAt(index);
            _navigator.ItemRemoved(index);
            QueueChanged?.Invoke(this, EventArgs.Empty);

            if (!wasCurrent)
                return;

            if (Queue.Count > 0)
                PlaySongAtIndex(Math.Max(0, _navigator.CurrentIndex));
            else
                Stop();
        }

        #endregion

        #region Transport

        public void PlaySongAtIndex(int index)
        {
            if (index < 0 || index >= Queue.Count)
                return;

            // Guards against the stopped-event handler re-entering while we are mid-switch
            if (_isSwitchingTrack)
                return;

            _isSwitchingTrack = true;

            try
            {
                _navigator.SetCurrent(index);
                var song = Queue[index];
                CurrentSong = song;

                LoadFile(song.FilePath);
                Play();

                // Fired even when the same song restarts, so repeat-one plays are counted
                TrackStarted?.Invoke(this, song);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing '{CurrentSong?.Title ?? "Unknown"}': {ex.Message}");
                Stop();
                throw new PlaybackException(CurrentSong, ex);
            }
            finally
            {
                _isSwitchingTrack = false;
            }
        }

        public void PrepareAt(int index, TimeSpan position)
        {
            if (index < 0 || index >= Queue.Count)
                return;

            try
            {
                _navigator.SetCurrent(index);
                var song = Queue[index];
                CurrentSong = song;

                LoadFile(song.FilePath);
                CurrentPosition = position;

                OnPropertyChanged(nameof(CurrentPosition));
                OnPropertyChanged(nameof(TotalDuration));
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not restore '{Queue[index].Title}': {ex.Message}");
                Stop();
            }
        }

        public void Play()
        {
            // No audio is loaded after Stop() or once the queue has run out. Resuming has to
            // reload the track, otherwise pressing play would quietly do nothing.
            if (_wavePlayer == null)
            {
                if (CurrentIndex >= 0 && CurrentIndex < Queue.Count)
                    PlaySongAtIndex(CurrentIndex);
                else if (Queue.Count > 0)
                    PlaySongAtIndex(0);

                return;
            }

            _wavePlayer.Play();
            _positionTimer.Start();
            RaisePlaybackStateChanged();
        }

        public void Pause()
        {
            _wavePlayer?.Pause();
            _positionTimer.Stop();
            RaisePlaybackStateChanged();
        }

        public void Stop()
        {
            StopPlayback();
            RaisePlaybackStateChanged();
        }

        public void Seek(TimeSpan position)
        {
            CurrentPosition = position;
            OnPropertyChanged(nameof(CurrentPosition));
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PlayNext()
        {
            var next = _navigator.Next();
            if (next.HasValue)
                PlaySongAtIndex(next.Value);
            else
                Stop();
        }

        public void PlayPrevious()
        {
            var previous = _navigator.Previous();
            if (previous.HasValue)
                PlaySongAtIndex(previous.Value);
            else
                Stop();
        }

        public void SwapSource(string filePath)
        {
            var position = CurrentPosition;
            var wasPlaying = PlaybackState == PlaybackState.Playing;

            LoadFile(filePath);
            CurrentPosition = position;

            if (wasPlaying)
                Play();

            OnPropertyChanged(nameof(CurrentPosition));
            OnPropertyChanged(nameof(TotalDuration));
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Audio plumbing

        private void LoadFile(string filePath)
        {
            // Disposing the old player raises PlaybackStopped; that must not be mistaken for a
            // track finishing naturally, or every track change would skip a song.
            _suppressAutoAdvance = true;
            StopPlayback();

            try
            {
                _audioFileReader = new AudioFileReader(filePath) { Volume = _volume };
                _wavePlayer = new WaveOutEvent();
                _wavePlayer.Init(_audioFileReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;

                OnPropertyChanged(nameof(CurrentPosition));
                OnPropertyChanged(nameof(TotalDuration));
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load audio file: {ex.Message}", ex);
            }
            finally
            {
                _suppressAutoAdvance = false;
            }
        }

        private void StopPlayback()
        {
            _positionTimer.Stop();

            if (_wavePlayer != null)
                _wavePlayer.PlaybackStopped -= OnPlaybackStopped;

            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _audioFileReader?.Dispose();
            _wavePlayer = null;
            _audioFileReader = null;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // Ignore events from a player we have already replaced
            if (!ReferenceEquals(sender, _wavePlayer))
                return;

            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));

            if (e.Exception == null && !_suppressAutoAdvance)
            {
                try
                {
                    PlayNext();
                }
                catch (PlaybackException ex)
                {
                    // A broken file must not take the app down while advancing on its own
                    Debug.WriteLine($"Auto-advance failed: {ex.Message}");
                    Stop();
                }
            }

            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RaisePlaybackStateChanged()
        {
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public void Dispose()
        {
            if (_disposed)
                return;

            _positionTimer.Stop();
            StopPlayback();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Thrown when a track cannot be played. The service reports the failure instead of showing
    /// a message box, leaving the presentation choice to the caller.
    /// </summary>
    public class PlaybackException : Exception
    {
        public PlaybackException(Song? song, Exception inner)
            : base($"Error playing '{song?.Title ?? "Unknown"}': {inner.Message}", inner)
        {
            Song = song;
        }

        public Song? Song { get; }
    }
}
