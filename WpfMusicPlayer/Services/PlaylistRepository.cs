using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Stores playlists as JSON under %APPDATA%. Cover images are written as separate files:
    /// embedding them as base64 made the playlist file grow by hundreds of kilobytes per entry
    /// and it is rewritten every time a song is added.
    /// </summary>
    public class PlaylistRepository : IPlaylistRepository
    {
        private readonly string _playlistsFilePath;
        private readonly string _coversDirectory;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <param name="dataDirectory">
        /// Where playlists and covers are stored. Defaults to the folder under %APPDATA%.
        /// </param>
        public PlaylistRepository(string? dataDirectory = null)
        {
            var appFolder = dataDirectory ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfMusicPlayer");

            Directory.CreateDirectory(appFolder);

            _playlistsFilePath = Path.Combine(appFolder, "playlists.json");
            _coversDirectory = Path.Combine(appFolder, "covers");
            Directory.CreateDirectory(_coversDirectory);

            Load();
        }

        public ObservableCollection<Playlist> Playlists { get; } = new();

        public Playlist Create(string name, string description = "", byte[]? coverImage = null)
        {
            var playlist = new Playlist
            {
                Name = name,
                Description = description,
                CoverImage = coverImage
            };

            Playlists.Add(playlist);
            Save();

            return playlist;
        }

        public void Delete(Playlist playlist)
        {
            Playlists.Remove(playlist);

            var coverPath = GetCoverPath(playlist.Id);
            if (File.Exists(coverPath))
            {
                try
                {
                    File.Delete(coverPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not delete playlist cover: {ex.Message}");
                }
            }

            Save();
        }

        public void AddSong(Playlist playlist, Song song)
        {
            if (playlist.Songs.Any(s => s.Equals(song)))
                return;

            playlist.Songs.Add(song);
            Save();
        }

        public void RemoveSong(Playlist playlist, Song song)
        {
            playlist.Songs.Remove(song);
            Save();
        }

        public void RelinkTo(IEnumerable<Song> librarySongs)
        {
            var byPath = new Dictionary<string, Song>(StringComparer.OrdinalIgnoreCase);
            foreach (var song in librarySongs)
                byPath[song.FilePath] = song;

            foreach (var playlist in Playlists)
            {
                var linked = new List<Song>();

                foreach (var entry in playlist.Songs.ToList())
                {
                    if (byPath.TryGetValue(entry.FilePath, out var librarySong))
                        linked.Add(librarySong);
                    else if (File.Exists(entry.FilePath))
                        linked.Add(entry);
                    // Otherwise the file is gone; drop it from the playlist
                }

                playlist.Songs.Clear();
                foreach (var song in linked)
                    playlist.Songs.Add(song);
            }
        }

        public void Save()
        {
            try
            {
                var data = Playlists.Select(playlist =>
                {
                    SaveCover(playlist);

                    return new PersistedPlaylist
                    {
                        Id = playlist.Id,
                        Name = playlist.Name,
                        Description = playlist.Description,
                        CreatedDate = playlist.CreatedDate,
                        Songs = playlist.Songs.Select(s => new PersistedSong
                        {
                            FilePath = s.FilePath,
                            Title = s.Title,
                            Artist = s.Artist,
                            Album = s.Album,
                            Genre = s.Genre,
                            Year = s.Year,
                            DurationSeconds = s.Duration.TotalSeconds
                        }).ToList()
                    };
                }).ToList();

                File.WriteAllText(_playlistsFilePath, JsonSerializer.Serialize(data, SerializerOptions));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving playlists: {ex.Message}");
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_playlistsFilePath))
                    return;

                var json = File.ReadAllText(_playlistsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var data = JsonSerializer.Deserialize<List<PersistedPlaylist>>(json);
                if (data == null)
                    return;

                foreach (var persisted in data)
                {
                    var playlist = new Playlist
                    {
                        Id = string.IsNullOrEmpty(persisted.Id) ? Guid.NewGuid().ToString() : persisted.Id,
                        Name = string.IsNullOrEmpty(persisted.Name) ? "Untitled Playlist" : persisted.Name,
                        Description = persisted.Description ?? string.Empty,
                        CreatedDate = persisted.CreatedDate,
                        CoverImage = LoadCover(persisted.Id)
                    };

                    foreach (var song in persisted.Songs.Where(s => !string.IsNullOrEmpty(s.FilePath)))
                    {
                        // Artwork is intentionally not persisted; it is restored from the file's
                        // own tags once the library is scanned.
                        playlist.Songs.Add(new Song
                        {
                            FilePath = song.FilePath,
                            Title = song.Title ?? string.Empty,
                            Artist = song.Artist ?? string.Empty,
                            Album = song.Album ?? string.Empty,
                            Genre = song.Genre ?? string.Empty,
                            Year = song.Year,
                            Duration = TimeSpan.FromSeconds(song.DurationSeconds)
                        });
                    }

                    Playlists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading playlists: {ex.Message}");
            }
        }

        private string GetCoverPath(string playlistId) => Path.Combine(_coversDirectory, $"{playlistId}.img");

        private void SaveCover(Playlist playlist)
        {
            if (playlist.CoverImage is not { Length: > 0 })
                return;

            try
            {
                File.WriteAllBytes(GetCoverPath(playlist.Id), playlist.CoverImage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not save playlist cover: {ex.Message}");
            }
        }

        private byte[]? LoadCover(string? playlistId)
        {
            if (string.IsNullOrEmpty(playlistId))
                return null;

            try
            {
                var path = GetCoverPath(playlistId);
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not load playlist cover: {ex.Message}");
                return null;
            }
        }

        private class PersistedPlaylist
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime CreatedDate { get; set; } = DateTime.Now;
            public List<PersistedSong> Songs { get; set; } = new();
        }

        private class PersistedSong
        {
            public string FilePath { get; set; } = string.Empty;
            public string? Title { get; set; }
            public string? Artist { get; set; }
            public string? Album { get; set; }
            public string? Genre { get; set; }
            public int Year { get; set; }
            public double DurationSeconds { get; set; }
        }
    }
}
