using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Owns the song library: scanning folders, reading tags and searching.
    /// </summary>
    public class MusicLibraryService : IMusicLibraryService
    {
        private static readonly string[] SupportedExtensions =
            { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" };

        /// <summary>Songs are handed to the UI in batches; one dispatch per file is wasteful.</summary>
        private const int BatchSize = 50;

        private readonly IListeningStatsService _statsService;
        private readonly IMoodAnalyzerService _moodService;

        public MusicLibraryService(IListeningStatsService statsService, IMoodAnalyzerService moodService)
        {
            _statsService = statsService;
            _moodService = moodService;
        }

        public ObservableCollection<Song> Songs { get; } = new();

        public async Task ScanFolderAsync(string folderPath, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(folderPath))
                return;

            await Task.Run(() =>
            {
                var files = EnumerateAudioFiles(folderPath);
                var batch = new List<Song>(BatchSize);

                for (var i = 0; i < files.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var song = LoadSongFromFile(files[i]);
                    if (song != null)
                        batch.Add(song);

                    if (batch.Count >= BatchSize)
                    {
                        AddSongs(batch);
                        batch.Clear();
                    }

                    progress?.Report($"Scanning... {i + 1}/{files.Count}");
                }

                if (batch.Count > 0)
                    AddSongs(batch);

                progress?.Report($"Found {Songs.Count} songs");
            }, cancellationToken);
        }

        public async Task AddFilesAsync(IEnumerable<string> filePaths)
        {
            var paths = filePaths.ToList();

            await Task.Run(() =>
            {
                var batch = new List<Song>();

                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        batch.AddRange(EnumerateAudioFiles(path)
                            .Select(LoadSongFromFile)
                            .Where(s => s != null)!);
                    }
                    else if (File.Exists(path) && IsSupported(path))
                    {
                        var song = LoadSongFromFile(path);
                        if (song != null)
                            batch.Add(song);
                    }
                }

                if (batch.Count > 0)
                    AddSongs(batch);
            });
        }

        public IReadOnlyList<Song> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Songs.ToList();

            query = query.ToLowerInvariant();

            return Songs.Where(s =>
                s.Title.ToLowerInvariant().Contains(query) ||
                s.Artist.ToLowerInvariant().Contains(query) ||
                s.Album.ToLowerInvariant().Contains(query) ||
                s.Genre.ToLowerInvariant().Contains(query)).ToList();
        }

        public void UpdateMetadata(Song song, bool albumArtChanged = false, byte[]? replacementAlbumArt = null)
        {
            try
            {
                using var file = TagLib.File.Create(song.FilePath);

                file.Tag.Title = song.Title;
                file.Tag.Performers = new[] { song.Artist };
                file.Tag.Album = song.Album;
                file.Tag.Genres = string.IsNullOrWhiteSpace(song.Genre) ? Array.Empty<string>() : new[] { song.Genre };
                file.Tag.Year = song.Year > 0 ? (uint)song.Year : 0;

                if (albumArtChanged)
                {
                    if (replacementAlbumArt is { Length: > 0 })
                    {
                        file.Tag.Pictures = new TagLib.IPicture[]
                        {
                            new TagLib.Picture(replacementAlbumArt)
                            {
                                Type = TagLib.PictureType.FrontCover,
                                Description = "Album Art"
                            }
                        };
                    }
                    else
                    {
                        file.Tag.Pictures = Array.Empty<TagLib.IPicture>();
                    }
                }

                file.Save();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to update metadata for '{song.Title}': {ex.Message}", ex);
            }
        }

        private static List<string> EnumerateAudioFiles(string folderPath)
        {
            try
            {
                return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                    .Where(IsSupported)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error enumerating '{folderPath}': {ex.Message}");
                return new List<string>();
            }
        }

        private static bool IsSupported(string filePath) =>
            SupportedExtensions.Contains(Path.GetExtension(filePath).ToLowerInvariant());

        private Song? LoadSongFromFile(string filePath)
        {
            try
            {
                using var file = TagLib.File.Create(filePath);

                var song = new Song
                {
                    FilePath = filePath,
                    Title = string.IsNullOrEmpty(file.Tag.Title)
                        ? Path.GetFileNameWithoutExtension(filePath)
                        : file.Tag.Title,
                    Artist = string.IsNullOrEmpty(file.Tag.FirstPerformer) ? "Unknown Artist" : file.Tag.FirstPerformer,
                    Album = string.IsNullOrEmpty(file.Tag.Album) ? "Unknown Album" : file.Tag.Album,
                    Genre = string.IsNullOrEmpty(file.Tag.FirstGenre) ? "Unknown" : file.Tag.FirstGenre,
                    Year = (int)file.Tag.Year,
                    Duration = file.Properties.Duration
                };

                if (file.Tag.Pictures.Length > 0)
                    song.AlbumArt = ImageUtils.CreateThumbnail(file.Tag.Pictures[0].Data.Data);

                song.ViewCount = _statsService.GetPlayCount(song.FilePath);

                // A previously analysed mood is restored from cache rather than recomputed
                var mood = _moodService.GetMood(song.FilePath);
                if (mood != null)
                {
                    song.Mood = mood.Mood;
                    song.MoodDetails = mood.Describe();
                }

                return song;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading file '{filePath}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a batch to the observable collection on the UI thread. Falls back to a direct add
        /// when there is no Application running.
        /// </summary>
        private void AddSongs(IReadOnlyList<Song> songs)
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher == null || dispatcher.CheckAccess())
            {
                MergeSongs(songs);
                return;
            }

            dispatcher.Invoke(() => MergeSongs(songs));
        }

        private void MergeSongs(IReadOnlyList<Song> songs)
        {
            var known = new HashSet<string>(
                Songs.Select(s => s.FilePath.ToLowerInvariant()));

            foreach (var song in songs)
            {
                if (known.Add(song.FilePath.ToLowerInvariant()))
                    Songs.Add(song);
            }
        }
    }
}
