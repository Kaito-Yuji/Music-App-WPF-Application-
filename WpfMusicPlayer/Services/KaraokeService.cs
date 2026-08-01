using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Coordinates stem separation with playback: swapping the audio source for the instrumental
    /// stem and back again, and exporting stems to disk.
    /// </summary>
    public class KaraokeService : IKaraokeService
    {
        private readonly IAudioSeparatorService _separator;
        private readonly IPlaybackService _playback;

        private bool _isInKaraokeMode;

        public KaraokeService(IAudioSeparatorService separator, IPlaybackService playback)
        {
            _separator = separator;
            _playback = playback;

            _separator.ProgressChanged += (_, args) => Progress?.Invoke(this, args);

            // Moving to another track leaves karaoke mode behind
            _playback.CurrentSongChanged += (_, _) => IsInKaraokeMode = false;
        }

        public event EventHandler<AudioProgressEventArgs>? Progress;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsAvailable => _separator.IsAvailable;

        public bool IsInKaraokeMode
        {
            get => _isInKaraokeMode;
            private set
            {
                if (_isInKaraokeMode == value)
                    return;

                _isInKaraokeMode = value;
                OnPropertyChanged();
            }
        }

        public bool StemsExist(Song song) => _separator.StemsExist(song.FilePath);

        public Task<DependencyCheck> CheckDependenciesAsync() =>
            _separator.CheckDependenciesAsync();

        public async Task<bool> EnableAsync(CancellationToken cancellationToken = default)
        {
            var song = _playback.CurrentSong;
            if (song == null)
                return false;

            try
            {
                if (!_separator.StemsExist(song.FilePath))
                {
                    var separated = await _separator.SeparateAudioAsync(song.FilePath, cancellationToken);
                    if (!separated)
                        return false;
                }

                var (_, accompanimentPath) = _separator.GetStemPaths(song.FilePath);

                if (!File.Exists(accompanimentPath))
                {
                    ReportError($"Instrumental file not found: {accompanimentPath}");
                    return false;
                }

                _playback.SwapSource(accompanimentPath);
                IsInKaraokeMode = true;
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Error switching to karaoke mode: {ex.Message}");
                return false;
            }
        }

        public bool Disable()
        {
            var song = _playback.CurrentSong;
            if (song == null || !IsInKaraokeMode)
                return false;

            try
            {
                _playback.SwapSource(song.FilePath);
                IsInKaraokeMode = false;
                return true;
            }
            catch (Exception ex)
            {
                ReportError($"Error switching back from karaoke mode: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ExportAsync(Song song, string outputPath, bool instrumental,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_separator.StemsExist(song.FilePath))
                {
                    var separated = await _separator.SeparateAudioAsync(song.FilePath, cancellationToken);
                    if (!separated)
                        return false;
                }

                var (vocalsPath, accompanimentPath) = _separator.GetStemPaths(song.FilePath);
                var sourcePath = instrumental ? accompanimentPath : vocalsPath;

                if (!File.Exists(sourcePath))
                    return false;

                File.Copy(sourcePath, outputPath, true);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                ReportError($"Error exporting stems: {ex.Message}");
                return false;
            }
        }

        public async Task PreprocessAsync(IEnumerable<Song> songs, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var pending = songs.Where(s => !_separator.StemsExist(s.FilePath)).ToList();

            for (var i = 0; i < pending.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report($"Separating {i + 1}/{pending.Count}: {pending[i].Title}");
                await _separator.SeparateAudioAsync(pending[i].FilePath, cancellationToken);
            }

            progress?.Report(pending.Count == 0
                ? "All selected songs already have stems"
                : $"Finished separating {pending.Count} songs");
        }

        private void ReportError(string message)
        {
            Debug.WriteLine(message);

            Progress?.Invoke(this, new AudioProgressEventArgs
            {
                Message = message,
                IsCompleted = true,
                IsError = true
            });
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
