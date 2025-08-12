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
                    _playMode = value;
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

        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
        {
            Queue.Clear();
            foreach (var song in songs)
                Queue.Add(song);

            CurrentIndex = Math.Max(0, Math.Min(startIndex, Queue.Count - 1));
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
                PlaySongAtIndex(Queue.Count - 1);
            }
        }

        public void PlaySongDirectly(Song song)
        {
            // Simple direct playback - just play the damn song!
            CurrentSong = song;
            
            try
            {
                _audioService.LoadFile(song.FilePath);
                _audioService.Play();
                _positionTimer.Start();
                
                OnPropertyChanged(nameof(PlaybackState));
                OnPropertyChanged(nameof(TotalDuration));
                PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing song: {ex.Message}");
            }
        }

        public void PlaySongAtIndex(int index)
        {
            if (index < 0 || index >= Queue.Count)
                return;

            CurrentIndex = index;
            CurrentSong = Queue[index];

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
                PlayMode.RepeatOne => CurrentIndex,
                PlayMode.Shuffle => _random.Next(Queue.Count),
                PlayMode.RepeatAll => (CurrentIndex + 1) % Queue.Count,
                _ => CurrentIndex + 1 < Queue.Count ? CurrentIndex + 1 : -1
            };
        }

        private int GetPreviousIndex()
        {
            if (Queue.Count == 0) return -1;

            return PlayMode switch
            {
                PlayMode.RepeatOne => CurrentIndex,
                PlayMode.Shuffle => _random.Next(Queue.Count),
                PlayMode.RepeatAll => CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : Queue.Count - 1,
                _ => CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : -1
            };
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
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveFromQueue(Song song)
        {
            var index = Queue.ToList().FindIndex(s => s.Id == song.Id);
            if (index >= 0)
            {
                Queue.RemoveAt(index);
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
                if (PlayMode == PlayMode.RepeatOne)
                {
                    PlaySongAtIndex(CurrentIndex);
                }
                else
                {
                    PlayNext();
                }
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
