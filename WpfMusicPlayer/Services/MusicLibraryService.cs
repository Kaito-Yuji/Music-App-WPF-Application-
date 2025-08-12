using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using WpfMusicPlayer.Models;
using TagLib;

namespace WpfMusicPlayer.Services
{
    public class MusicLibraryService
    {
        private readonly string[] _supportedExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" };
        
        public ObservableCollection<Song> Songs { get; } = new ObservableCollection<Song>();
        public ObservableCollection<Playlist> Playlists { get; } = new ObservableCollection<Playlist>();

        public event EventHandler<string>? ScanProgressChanged;

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
            return playlist;
        }

        public void DeletePlaylist(Playlist playlist)
        {
            Playlists.Remove(playlist);
        }

        public void AddSongToPlaylist(Playlist playlist, Song song)
        {
            if (!playlist.Songs.Contains(song))
                playlist.Songs.Add(song);
        }

        public void RemoveSongFromPlaylist(Playlist playlist, Song song)
        {
            playlist.Songs.Remove(song);
        }
    }
}
