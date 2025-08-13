using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public class PlayerService : IDisposable, INotifyPropertyChanged
    {
        private readonly AudioService _audioService;
        private readonly Random _random = new Random();
        private readonly DispatcherTimer _positionTimer;
        private bool _disposed = false;

        private Song? _currentSong;
        private PlayMode _playMode = PlayMode.Normal;
        private List<int> _shuffleOrder = new List<int>();
        private int _shuffleIndex = -1;

        public ObservableCollection<Song> Queue { get; } = new ObservableCollection<Song>();
        public Song? CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnPropertyChanged();
                    CurrentSongChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public int CurrentIndex { get; private set; } = -1;
        public PlayMode PlayMode
        {
            get => _playMode;
            set
            {
                if (_playMode != value)
                {
                    var oldMode = _playMode;
                    _playMode = value;
                    OnPlayModeChanged(oldMode, value);
                    OnPropertyChanged();
                }
            }
        }

        public PlaybackState PlaybackState
        {
            get => _audioService.PlaybackState;
        }
        public TimeSpan CurrentPosition
        {
            get => _audioService.CurrentPosition;
        }
        public TimeSpan TotalDuration
        {
            get => _audioService.TotalDuration;
        }
        public float Volume
        {
            get => _audioService.Volume;
            set => _audioService.Volume = value;
        }

        public event EventHandler? CurrentSongChanged;
        public event EventHandler? PlaybackStateChanged;
        public event EventHandler? PositionChanged;
        public event EventHandler? QueueChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PlayerService()
        {
            _audioService = new AudioService();
            _audioService.PlaybackStopped += OnPlaybackStopped;

            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentPosition));
                PositionChanged?.Invoke(this, EventArgs.Empty);
            };
        }

        private void OnPlayModeChanged(PlayMode oldMode, PlayMode newMode)
        {
            // Handle shuffle mode changes
            if (newMode == PlayMode.Shuffle && oldMode != PlayMode.Shuffle)
            {
                GenerateShuffleOrder();
            }
            else if (oldMode == PlayMode.Shuffle && newMode != PlayMode.Shuffle)
            {
                // Reset shuffle state when leaving shuffle mode
                _shuffleOrder.Clear();
                _shuffleIndex = -1;
            }
        }

        private void GenerateShuffleOrder()
        {
            if (Queue.Count == 0) return;

            _shuffleOrder.Clear();
            _shuffleOrder.AddRange(Enumerable.Range(0, Queue.Count));

            // Fisher-Yates shuffle
            for (int i = _shuffleOrder.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
            }

            // Find current song in shuffle order
            _shuffleIndex = _shuffleOrder.FindIndex(index => index == CurrentIndex);
            if (_shuffleIndex == -1 && CurrentIndex >= 0)
            {
                // Current song not found, add it at the beginning
                _shuffleOrder.Insert(0, CurrentIndex);
                _shuffleIndex = 0;
            }
        }

        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
        {
            Queue.Clear();
            foreach (var song in songs)
                Queue.Add(song);

            CurrentIndex = Math.Max(0, Math.Min(startIndex, Queue.Count - 1));

            // Regenerate shuffle order if in shuffle mode
            if (PlayMode == PlayMode.Shuffle)
            {
                GenerateShuffleOrder();
            }

            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PlaySong(Song song)
        {
            var index = Queue.ToList().FindIndex(s => s.Id == song.Id);
            if (index >= 0)
            {
                PlaySongAtIndex(index);
            }
            else
            {
                // Add to queue and play
                Queue.Add(song);
                SetQueue(Queue, Queue.Count - 1);
                PlaySongAtIndex(Queue.Count - 1);
            }
        }

        public void PlaySongDirectly(Song song)
        {
            // Create a single-song queue and play it
            SetQueue(new[] { song }, 0);
            PlaySongAtIndex(0);
        }

        public void PlaySongFromCollection(Song song, IEnumerable<Song> collection)
        {
            // Set the entire collection as queue and play the specific song
            var songList = collection.ToList();
            var index = songList.FindIndex(s => s.Id == song.Id);

            if (index >= 0)
            {
                SetQueue(songList, index);
                PlaySongAtIndex(index);
            }
        }

        public void PlaySongAtIndex(int index)
        {
            if (index < 0 || index >= Queue.Count)
                return;

            CurrentIndex = index;
            CurrentSong = Queue[index];

            // Update shuffle index if in shuffle mode
            if (PlayMode == PlayMode.Shuffle)
            {
                _shuffleIndex = _shuffleOrder.FindIndex(i => i == index);
                if (_shuffleIndex == -1)
                {
                    // Index not in shuffle order, regenerate
                    GenerateShuffleOrder();
                }
            }

            try
            {
                _audioService.LoadFile(CurrentSong.FilePath);
                _audioService.Play();
                _positionTimer.Start();

                OnPropertyChanged(nameof(PlaybackState));
                OnPropertyChanged(nameof(TotalDuration));
                PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing song: {ex.Message}");
                // Try to play next song
                PlayNext();
            }
        }

        public void Play()
        {
            if (CurrentSong == null && Queue.Count > 0)
            {
                PlaySongAtIndex(0);
                return;
            }

            _audioService.Play();
            _positionTimer.Start();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            _audioService.Pause();
            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            _audioService.Stop();
            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void PlayNext()
        {
            int nextIndex = GetNextIndex();
            if (nextIndex >= 0)
                PlaySongAtIndex(nextIndex);
        }

        public void PlayPrevious()
        {
            int prevIndex = GetPreviousIndex();
            if (prevIndex >= 0)
                PlaySongAtIndex(prevIndex);
        }

        private int GetNextIndex()
        {
            if (Queue.Count == 0) return -1;

            return PlayMode switch
            {
                PlayMode.RepeatOne => CurrentIndex, // Stay on same song
                PlayMode.Shuffle => GetNextShuffleIndex(),
                PlayMode.RepeatAll => (CurrentIndex + 1) % Queue.Count, // Loop back to start
                _ => CurrentIndex + 1 < Queue.Count ? CurrentIndex + 1 : -1 // Normal: stop at end
            };
        }

        private int GetPreviousIndex()
        {
            if (Queue.Count == 0) return -1;

            return PlayMode switch
            {
                PlayMode.RepeatOne => CurrentIndex,
                PlayMode.Shuffle => GetPreviousShuffleIndex(),
                PlayMode.RepeatAll => CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : Queue.Count - 1,
                _ => CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : -1
            };
        }

        private int GetNextShuffleIndex()
        {
            if (_shuffleOrder.Count == 0)
            {
                GenerateShuffleOrder();
                if (_shuffleOrder.Count == 0) return -1;
            }

            _shuffleIndex++;
            if (_shuffleIndex >= _shuffleOrder.Count)
            {
                // End of shuffle order
                if (PlayMode == PlayMode.Shuffle)
                {
                    // In shuffle mode, regenerate order for continuous play
                    GenerateShuffleOrder();
                    _shuffleIndex = 0;
                }
                else
                {
                    return -1; // No more songs
                }
            }

            return _shuffleIndex < _shuffleOrder.Count ? _shuffleOrder[_shuffleIndex] : -1;
        }

        private int GetPreviousShuffleIndex()
        {
            if (_shuffleOrder.Count == 0)
            {
                GenerateShuffleOrder();
                if (_shuffleOrder.Count == 0) return -1;
            }

            _shuffleIndex--;
            if (_shuffleIndex < 0)
            {
                _shuffleIndex = _shuffleOrder.Count - 1;
            }

            return _shuffleIndex >= 0 ? _shuffleOrder[_shuffleIndex] : -1;
        }

        public void Seek(TimeSpan position)
        {
            _audioService.CurrentPosition = position;
            OnPropertyChanged(nameof(CurrentPosition));
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void AddToQueue(Song song)
        {
            Queue.Add(song);

            // Update shuffle order if in shuffle mode
            if (PlayMode == PlayMode.Shuffle)
            {
                _shuffleOrder.Add(Queue.Count - 1);
                // Shuffle the new addition into the remaining unplayed songs
                var remainingStart = _shuffleIndex + 1;
                if (remainingStart < _shuffleOrder.Count - 1)
                {
                    var newIndex = _random.Next(remainingStart, _shuffleOrder.Count);
                    (_shuffleOrder[_shuffleOrder.Count - 1], _shuffleOrder[newIndex]) =
                        (_shuffleOrder[newIndex], _shuffleOrder[_shuffleOrder.Count - 1]);
                }
            }

            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveFromQueue(Song song)
        {
            var index = Queue.ToList().FindIndex(s => s.Id == song.Id);
            if (index >= 0)
            {
                Queue.RemoveAt(index);

                // Update shuffle order if in shuffle mode
                if (PlayMode == PlayMode.Shuffle)
                {
                    var shuffleOrderIndex = _shuffleOrder.FindIndex(i => i == index);
                    if (shuffleOrderIndex >= 0)
                    {
                        _shuffleOrder.RemoveAt(shuffleOrderIndex);
                        if (shuffleOrderIndex <= _shuffleIndex) _shuffleIndex--;
                    }

                    // Adjust all indices greater than the removed index
                    for (int i = 0; i < _shuffleOrder.Count; i++)
                    {
                        if (_shuffleOrder[i] > index) _shuffleOrder[i]--;
                    }
                }

                if (index < CurrentIndex)
                    CurrentIndex--;
                else if (index == CurrentIndex)
                {
                    // Current song was removed, play next or stop
                    if (Queue.Count > 0)
                        PlaySongAtIndex(Math.Min(CurrentIndex, Queue.Count - 1));
                    else
                        Stop();
                }
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnPlaybackStopped(object? sender, PlaybackStoppedEventArgs e)
        {
            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));

            if (e.Exception == null) // Natural end of song
            {
                // Auto-advance to next song based on play mode
                var nextIndex = GetNextIndex();
                if (nextIndex >= 0 && nextIndex != CurrentIndex) // Prevent infinite loop on single song
                {
                    System.Diagnostics.Debug.WriteLine($"Auto-advancing from song {CurrentIndex} to {nextIndex}");
                    PlaySongAtIndex(nextIndex);
                }
                else if (PlayMode == PlayMode.RepeatAll && Queue.Count > 0)
                {
                    // If we're at the end in RepeatAll mode, go back to beginning
                    System.Diagnostics.Debug.WriteLine("RepeatAll: Restarting from beginning");
                    PlaySongAtIndex(0);
                }
                else if (PlayMode == PlayMode.RepeatOne)
                {
                    // Repeat the same song
                    System.Diagnostics.Debug.WriteLine("RepeatOne: Restarting current song");
                    PlaySongAtIndex(CurrentIndex);
                }
                // If no next song and not in repeat mode, just stop
            }

            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _positionTimer?.Stop();
                _audioService?.Dispose();
                _disposed = true;
            }
        }
    }
}
