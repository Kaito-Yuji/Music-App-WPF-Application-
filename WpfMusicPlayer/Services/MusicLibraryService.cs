using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using WpfMusicPlayer.Models;
using TagLib;

namespace WpfMusicPlayer.Services
{
    public class MusicLibraryService
    {
        private readonly string[] _supportedExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" };
        private readonly string _playlistsFilePath;
        
        public ObservableCollection<Song> Songs { get; } = new ObservableCollection<Song>();
        public ObservableCollection<Playlist> Playlists { get; } = new ObservableCollection<Playlist>();

        public event EventHandler<string>? ScanProgressChanged;

        public MusicLibraryService()
        {
            // Store playlists in AppData folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WpfMusicPlayer");
            Directory.CreateDirectory(appFolder);
            _playlistsFilePath = Path.Combine(appFolder, "playlists.json");
            
            LoadPlaylistsFromFile();
        }

        public async Task ScanFolderAsync(string folderPath)
        {
            await Task.Run(() => ScanFolder(folderPath));
        }

        private void ScanFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(file => _supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();

            for (int i = 0; i < files.Count; i++)
            {
                try
                {
                    var song = LoadSongFromFile(files[i]);
                    if (song != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (!Songs.Any(s => s.FilePath == song.FilePath))
                                Songs.Add(song);
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    System.Diagnostics.Debug.WriteLine($"Error loading file {files[i]}: {ex.Message}");
                }

                ScanProgressChanged?.Invoke(this, $"Scanning... {i + 1}/{files.Count}");
            }

            ScanProgressChanged?.Invoke(this, "Scan completed");
        }

        private Song? LoadSongFromFile(string filePath)
        {
            try
            {
                using var file = TagLib.File.Create(filePath);
                var song = new Song
                {
                    FilePath = filePath,
                    Title = string.IsNullOrEmpty(file.Tag.Title) ? Path.GetFileNameWithoutExtension(filePath) : file.Tag.Title,
                    Artist = string.IsNullOrEmpty(file.Tag.FirstPerformer) ? "Unknown Artist" : file.Tag.FirstPerformer,
                    Album = string.IsNullOrEmpty(file.Tag.Album) ? "Unknown Album" : file.Tag.Album,
                    Genre = string.IsNullOrEmpty(file.Tag.FirstGenre) ? "Unknown" : file.Tag.FirstGenre,
                    Year = (int)file.Tag.Year,
                    Duration = file.Properties.Duration
                };

                // Extract album art
                if (file.Tag.Pictures.Length > 0)
                {
                    song.AlbumArt = file.Tag.Pictures[0].Data.Data;
                }

                return song;
            }
            catch
            {
                return null;
            }
        }

        public List<Song> SearchSongs(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Songs.ToList();

            query = query.ToLowerInvariant();
            return Songs.Where(s =>
                s.Title.ToLowerInvariant().Contains(query) ||
                s.Artist.ToLowerInvariant().Contains(query) ||
                s.Album.ToLowerInvariant().Contains(query) ||
                s.Genre.ToLowerInvariant().Contains(query)
            ).ToList();
        }

        public Playlist CreatePlaylist(string name, string description = "", byte[]? coverImage = null)
        {
            var playlist = new Playlist 
            { 
                Name = name,
                Description = description,
                CoverImage = coverImage
            };
            Playlists.Add(playlist);
            SavePlaylistsToFile();
            return playlist;
        }

        public void DeletePlaylist(Playlist playlist)
        {
            Playlists.Remove(playlist);
            SavePlaylistsToFile();
        }

        public void AddSongToPlaylist(Playlist playlist, Song song)
        {
            if (!playlist.Songs.Contains(song))
            {
                playlist.Songs.Add(song);
                SavePlaylistsToFile();
            }
        }

        public void RemoveSongFromPlaylist(Playlist playlist, Song song)
        {
            playlist.Songs.Remove(song);
            SavePlaylistsToFile();
        }

        public void UpdatePlaylist(Playlist playlist)
        {
            SavePlaylistsToFile();
        }

        private void SavePlaylistsToFile()
        {
            try
            {
                var playlistData = Playlists.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.CoverImage,
                    p.CreatedDate,
                    Songs = p.Songs.Select(s => new
                    {
                        s.Id,
                        s.Title,
                        s.Artist,
                        s.Album,
                        s.Genre,
                        s.Year,
                        s.FilePath,
                        Duration = s.Duration.TotalSeconds,
                        s.AlbumArt
                    }).ToList()
                }).ToList();

                var json = JsonSerializer.Serialize(playlistData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                System.IO.File.WriteAllText(_playlistsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving playlists: {ex.Message}");
            }
        }

        private void LoadPlaylistsFromFile()
        {
            try
            {
                if (!System.IO.File.Exists(_playlistsFilePath))
                    return;

                var json = System.IO.File.ReadAllText(_playlistsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                foreach (var playlistElement in root.EnumerateArray())
                {
                    var playlist = new Playlist
                    {
                        Id = playlistElement.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString(),
                        Name = playlistElement.GetProperty("Name").GetString() ?? "Untitled Playlist",
                        Description = playlistElement.GetProperty("Description").GetString() ?? "",
                        CreatedDate = playlistElement.GetProperty("CreatedDate").GetDateTime()
                    };

                    // Load cover image if exists
                    if (playlistElement.TryGetProperty("CoverImage", out var coverImageElement) && 
                        coverImageElement.ValueKind != JsonValueKind.Null)
                    {
                        var base64String = coverImageElement.GetString();
                        if (!string.IsNullOrEmpty(base64String))
                        {
                            playlist.CoverImage = Convert.FromBase64String(base64String);
                        }
                    }

                    // Load songs
                    if (playlistElement.TryGetProperty("Songs", out var songsElement))
                    {
                        foreach (var songElement in songsElement.EnumerateArray())
                        {
                            var song = new Song
                            {
                                Id = songElement.GetProperty("Id").GetString() ?? Guid.NewGuid().ToString(),
                                Title = songElement.GetProperty("Title").GetString() ?? "Unknown Title",
                                Artist = songElement.GetProperty("Artist").GetString() ?? "Unknown Artist",
                                Album = songElement.GetProperty("Album").GetString() ?? "Unknown Album",
                                Genre = songElement.GetProperty("Genre").GetString() ?? "Unknown",
                                Year = songElement.GetProperty("Year").GetInt32(),
                                FilePath = songElement.GetProperty("FilePath").GetString() ?? "",
                                Duration = TimeSpan.FromSeconds(songElement.GetProperty("Duration").GetDouble())
                            };

                            // Load album art if exists
                            if (songElement.TryGetProperty("AlbumArt", out var albumArtElement) && 
                                albumArtElement.ValueKind != JsonValueKind.Null)
                            {
                                var base64String = albumArtElement.GetString();
                                if (!string.IsNullOrEmpty(base64String))
                                {
                                    song.AlbumArt = Convert.FromBase64String(base64String);
                                }
                            }

                            // Only add the song if the file still exists
                            if (System.IO.File.Exists(song.FilePath))
                            {
                                playlist.Songs.Add(song);
                            }
                        }
                    }

                    Playlists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading playlists: {ex.Message}");
            }
        }
    }
}
