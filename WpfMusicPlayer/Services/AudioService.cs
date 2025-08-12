using NAudio.Wave;
using System;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public class AudioService : IDisposable
    {
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioFileReader;
        private bool _disposed = false;

        public event EventHandler<PlaybackStoppedEventArgs>? PlaybackStopped;
        public event EventHandler? PositionChanged;

        public Models.PlaybackState PlaybackState
        {
            get
            {
                if (_wavePlayer == null)
                    return Models.PlaybackState.Stopped;

                return _wavePlayer.PlaybackState switch
                {
                    NAudio.Wave.PlaybackState.Playing => Models.PlaybackState.Playing,
                    NAudio.Wave.PlaybackState.Paused => Models.PlaybackState.Paused,
                    _ => Models.PlaybackState.Stopped
                };
            }
        }

        public TimeSpan CurrentPosition
        {
            get => _audioFileReader?.CurrentTime ?? TimeSpan.Zero;
            set
            {
                if (_audioFileReader != null)
                    _audioFileReader.CurrentTime = value;
            }
        }

        public TimeSpan TotalDuration => _audioFileReader?.TotalTime ?? TimeSpan.Zero;

        public float Volume
        {
            get => _audioFileReader?.Volume ?? 1.0f;
            set
            {
                if (_audioFileReader != null)
                    _audioFileReader.Volume = Math.Max(0, Math.Min(1, value));
            }
        }

        public void LoadFile(string filePath)
        {
            Stop();
            
            try
            {
                _audioFileReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _wavePlayer.Init(_audioFileReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Could not load audio file: {ex.Message}", ex);
            }
        }

        public void Play()
        {
            _wavePlayer?.Play();
        }

        public void Pause()
        {
            _wavePlayer?.Pause();
        }

        public void Stop()
        {
            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _audioFileReader?.Dispose();
            _wavePlayer = null;
            _audioFileReader = null;
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, new PlaybackStoppedEventArgs { Exception = e.Exception });
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
                Stop();
                _disposed = true;
            }
        }
    }

    public class PlaybackStoppedEventArgs : EventArgs
    {
        public Exception? Exception { get; set; }
    }
}
