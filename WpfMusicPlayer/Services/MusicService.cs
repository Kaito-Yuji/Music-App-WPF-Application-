using NAudio.Wave;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TagLib;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public class MusicService : IDisposable, INotifyPropertyChanged
    {
        #region Private Fields
        private readonly string[] _supportedExtensions = { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma" };
        private readonly string _playlistsFilePath;
        private readonly Random _random = new Random();
        private readonly DispatcherTimer _positionTimer;
        private readonly ListeningStatsService _listeningStatsService;
        private readonly AudioSeparatorService _audioSeparatorService;
        private bool _isLoadingNewSong = false; // Flag to prevent auto-advance when loading new song
        private bool _isSwitchingTrack = false;

        // Audio components
        private IWavePlayer? _wavePlayer;
        private AudioFileReader? _audioFileReader;

        // Player state
        private Song? _currentSong;
        private List<int> _shuffleOrder = new List<int>();
        private int _shuffleIndex = -1;
        private bool _disposed = false;
        #endregion

        #region Public Properties
        public ObservableCollection<Song> Songs { get; } = new ObservableCollection<Song>();
        public ObservableCollection<Playlist> Playlists { get; } = new ObservableCollection<Playlist>();
        public ObservableCollection<Song> Queue { get; } = new ObservableCollection<Song>();

        public Song? CurrentSong
        {
            get => _currentSong;
            private set
            {
                if (_currentSong != value)
                {
                    // Stop tracking the previous song
                    if (_currentSong != null)
                    {
                        _listeningStatsService.OnSongStopped();
                    }
                    
                    _currentSong = value;
                    OnPropertyChanged();
                    CurrentSongChanged?.Invoke(this, EventArgs.Empty);
                    
                    // Start tracking the new song
                    if (_currentSong != null)
                    {
                        _listeningStatsService.OnSongStarted(_currentSong);
                    }
                }
            }
        }

        public int CurrentIndex { get; private set; } = -1;

        // Spotify-like separate controls
        private bool _isShuffleOn = false;
        private RepeatMode _repeatMode = RepeatMode.Off;

        public bool IsShuffleOn
        {
            get => _isShuffleOn;
            set
            {
                if (_isShuffleOn != value)
                {
                    _isShuffleOn = value;
                    OnShuffleChanged(value);
                    OnPropertyChanged();
                }
            }
        }

        public RepeatMode RepeatMode
        {
            get => _repeatMode;
            set
            {
                if (_repeatMode != value)
                {
                    _repeatMode = value;
                    OnPropertyChanged();
                }
            }
        }

        // Keep PlayMode for backward compatibility but make it computed
        public PlayMode PlayMode
        {
            get
            {
                if (IsShuffleOn && RepeatMode == RepeatMode.RepeatOne)
                    return PlayMode.RepeatOne;
                else if (IsShuffleOn && RepeatMode == RepeatMode.RepeatAll)
                    return PlayMode.Shuffle; // Shuffle with repeat all
                else if (IsShuffleOn)
                    return PlayMode.Shuffle;
                else if (RepeatMode == RepeatMode.RepeatOne)
                    return PlayMode.RepeatOne;
                else if (RepeatMode == RepeatMode.RepeatAll)
                    return PlayMode.RepeatAll;
                else
                    return PlayMode.Normal;
            }
            set
            {
                // Handle backward compatibility
                switch (value)
                {
                    case PlayMode.Normal:
                        IsShuffleOn = false;
                        RepeatMode = RepeatMode.Off;
                        break;
                    case PlayMode.RepeatOne:
                        IsShuffleOn = false;
                        RepeatMode = RepeatMode.RepeatOne;
                        break;
                    case PlayMode.RepeatAll:
                        IsShuffleOn = false;
                        RepeatMode = RepeatMode.RepeatAll;
                        break;
                    case PlayMode.Shuffle:
                        IsShuffleOn = true;
                        RepeatMode = RepeatMode.Off;
                        break;
                }
            }
        }

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

        // Karaoke mode properties
        private bool _isKaraokeMode = false;
        public bool IsKaraokeMode
        {
            get => _isKaraokeMode;
            set
            {
                if (_isKaraokeMode != value)
                {
                    _isKaraokeMode = value;
                    OnPropertyChanged();
                    
                    // Switch to karaoke mode if a song is currently playing
                    if (CurrentSong != null)
                    {
                        _ = SwitchToKaraokeModeAsync();
                    }
                }
            }
        }

        private string? _currentKaraokeFilePath;
        
        /// <summary>
        /// Gets whether the audio separator is available and ready to use
        /// </summary>
        public bool IsAudioSeparatorAvailable => _audioSeparatorService.IsAvailable;

        /// <summary>
        /// Checks if separated stems already exist for a given audio file
        /// </summary>
        /// <param name="inputFilePath">Original audio file path</param>
        /// <param name="outputDirectory">Output directory to check</param>
        /// <returns>True if both stems exist, false otherwise</returns>
        public bool CheckIfStemsExist(string inputFilePath, string outputDirectory)
        {
            return _audioSeparatorService.StemsExist(inputFilePath, outputDirectory);
        }

        /// <summary>
        /// Gets debug information about the audio separator setup
        /// </summary>
        /// <returns>String containing debug information</returns>
        public string GetAudioSeparatorSetupInfo()
        {
            return _audioSeparatorService.GetSetupInfo();
        }
        #endregion

        #region Events
        public event EventHandler? CurrentSongChanged;
        public event EventHandler? PlaybackStateChanged;
        public event EventHandler? PositionChanged;
        public event EventHandler? QueueChanged;
        public event EventHandler<string>? ScanProgressChanged;
        public event EventHandler<AudioSeparatorService.ProgressEventArgs>? AudioSeparationProgress;
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Constructor
        public MusicService()
        {
            // Initialize listening stats service
            _listeningStatsService = new ListeningStatsService();
            
            // Initialize audio separator service
            _audioSeparatorService = new AudioSeparatorService();
            
            // Subscribe to audio separator progress events
            _audioSeparatorService.ProgressChanged += (sender, args) =>
            {
                AudioSeparationProgress?.Invoke(this, args);
            };
            
            // Initialize playlists file path
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WpfMusicPlayer");
            Directory.CreateDirectory(appFolder);
            _playlistsFilePath = Path.Combine(appFolder, "playlists.json");

            // Initialize position timer
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentPosition));
                PositionChanged?.Invoke(this, EventArgs.Empty);
                
                // Track listening progress
                if (CurrentSong != null)
                {
                    _listeningStatsService.OnPositionUpdate(CurrentSong, CurrentPosition, TotalDuration);
                }
            };

            LoadPlaylistsFromFile();
        }
        #endregion

        #region Music Library Methods
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
                    System.Diagnostics.Debug.WriteLine($"Error loading file {files[i]}: {ex.Message}");
                }

                ScanProgressChanged?.Invoke(this, $"Scanning... {i + 1}/{files.Count}");
            }

            // After scanning is complete, relink playlists to use the actual song objects
            Application.Current.Dispatcher.Invoke(() =>
            {
                RelinkPlaylistSongs();
            });

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

        public void UpdateSongMetadata(Song song)
        {
            try
            {
                // Update the song metadata in the file using TagLib
                using var file = TagLib.File.Create(song.FilePath);
                
                // Update basic metadata
                file.Tag.Title = song.Title;
                file.Tag.Performers = new[] { song.Artist };
                file.Tag.Album = song.Album;
                file.Tag.Genres = string.IsNullOrWhiteSpace(song.Genre) ? new string[0] : new[] { song.Genre };
                file.Tag.Year = song.Year > 0 ? (uint)song.Year : 0;

                // Update album art if provided
                if (song.AlbumArt != null && song.AlbumArt.Length > 0)
                {
                    var picture = new TagLib.Picture(song.AlbumArt)
                    {
                        Type = TagLib.PictureType.FrontCover,
                        Description = "Album Art"
                    };
                    file.Tag.Pictures = new[] { picture };
                }
                else
                {
                    // Remove album art if set to null
                    file.Tag.Pictures = new TagLib.Picture[0];
                }

                // Save the changes to the file
                file.Save();
            }
            catch (Exception ex)
            {
                // If file updating fails, throw to let the caller handle it
                throw new InvalidOperationException($"Failed to update metadata for '{song.Title}': {ex.Message}", ex);
            }
        }
        #endregion

        #region Playlist Methods
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
            // Check if the song is already in the playlist (uses the new equality comparison)
            if (!playlist.Songs.Any(s => s.Equals(song)))
            {
                // Try to find the existing song in our main collection to maintain reference
                var existingSong = Songs.FirstOrDefault(s => s.Equals(song));
                if (existingSong != null)
                {
                    playlist.Songs.Add(existingSong);
                }
                else
                {
                    playlist.Songs.Add(song);
                }
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

                    if (playlistElement.TryGetProperty("CoverImage", out var coverImageElement) && 
                        coverImageElement.ValueKind != JsonValueKind.Null)
                    {
                        var base64String = coverImageElement.GetString();
                        if (!string.IsNullOrEmpty(base64String))
                        {
                            playlist.CoverImage = Convert.FromBase64String(base64String);
                        }
                    }

                    if (playlistElement.TryGetProperty("Songs", out var songsElement))
                    {
                        foreach (var songElement in songsElement.EnumerateArray())
                        {
                            var songId = songElement.GetProperty("Id").GetString() ?? "";
                            var filePath = songElement.GetProperty("FilePath").GetString() ?? "";
                            
                            // First try to find the song in the existing Songs collection by ID
                            var existingSong = Songs.FirstOrDefault(s => s.Id == songId);
                            
                            // If not found by ID, try to find by file path
                            if (existingSong == null && !string.IsNullOrEmpty(filePath))
                            {
                                existingSong = Songs.FirstOrDefault(s => s.FilePath == filePath);
                            }
                            
                            if (existingSong != null && System.IO.File.Exists(existingSong.FilePath))
                            {
                                // Use the existing song object to maintain reference
                                playlist.Songs.Add(existingSong);
                            }
                            else
                            {
                                // Only create new song object if not found in existing collection
                                // This handles cases where the song file might have been moved or the library hasn't been scanned yet
                                var song = new Song
                                {
                                    Id = songId,
                                    Title = songElement.GetProperty("Title").GetString() ?? "Unknown Title",
                                    Artist = songElement.GetProperty("Artist").GetString() ?? "Unknown Artist",
                                    Album = songElement.GetProperty("Album").GetString() ?? "Unknown Album",
                                    Genre = songElement.GetProperty("Genre").GetString() ?? "Unknown",
                                    Year = songElement.GetProperty("Year").GetInt32(),
                                    FilePath = filePath,
                                    Duration = TimeSpan.FromSeconds(songElement.GetProperty("Duration").GetDouble())
                                };

                                if (songElement.TryGetProperty("AlbumArt", out var albumArtElement) && 
                                    albumArtElement.ValueKind != JsonValueKind.Null)
                                {
                                    var base64String = albumArtElement.GetString();
                                    if (!string.IsNullOrEmpty(base64String))
                                    {
                                        song.AlbumArt = Convert.FromBase64String(base64String);
                                    }
                                }

                                if (System.IO.File.Exists(song.FilePath))
                                {
                                    playlist.Songs.Add(song);
                                }
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

        private void RelinkPlaylistSongs()
        {
            foreach (var playlist in Playlists)
            {
                // Create a list to hold the properly linked songs
                var linkedSongs = new List<Song>();

                foreach (var playlistSong in playlist.Songs.ToList())
                {
                    // Try to find the corresponding song in our main Songs collection
                    var existingSong = Songs.FirstOrDefault(s => s.Equals(playlistSong));
                    
                    if (existingSong != null)
                    {
                        // Use the existing song object to maintain reference
                        linkedSongs.Add(existingSong);
                    }
                    else if (System.IO.File.Exists(playlistSong.FilePath))
                    {
                        // Keep the playlist song if the file exists but wasn't found in the main collection
                        linkedSongs.Add(playlistSong);
                    }
                    // Otherwise, skip the song (file doesn't exist)
                }

                // Replace the playlist songs with the properly linked ones
                playlist.Songs.Clear();
                foreach (var song in linkedSongs)
                {
                    playlist.Songs.Add(song);
                }
            }
        }
        #endregion

        #region Audio Control Methods
        private void LoadFile(string filePath)
        {
            _isLoadingNewSong = true; // Set flag to prevent auto-advance
            StopPlayback();
            
            try
            {
                _audioFileReader = new AudioFileReader(filePath);
                _wavePlayer = new WaveOutEvent();
                _wavePlayer.Init(_audioFileReader);
                _wavePlayer.PlaybackStopped += OnPlaybackStopped;
                
                // Explicitly reset position and notify UI
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
                _isLoadingNewSong = false; // Clear flag
            }
        }

        public void Play()
        {
            if (CurrentSong == null && Queue.Count > 0)
            {
                PlaySongAtIndex(0);
                return;
            }

            _wavePlayer?.Play();
            _positionTimer.Start();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            _wavePlayer?.Pause();
            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            StopPlayback();
            OnPropertyChanged(nameof(PlaybackState));
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void StopPlayback()
        {
            _positionTimer.Stop();
            _wavePlayer?.Stop();
            _wavePlayer?.Dispose();
            _audioFileReader?.Dispose();
            _wavePlayer = null;
            _audioFileReader = null;
        }

        public void Seek(TimeSpan position)
        {
            CurrentPosition = position;
            OnPropertyChanged(nameof(CurrentPosition));
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Queue and Playback Management
        public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
        {
            Queue.Clear();
            foreach (var song in songs)
                Queue.Add(song);

            CurrentIndex = Math.Max(0, Math.Min(startIndex, Queue.Count - 1));

            if (IsShuffleOn)
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
                Queue.Add(song);
                SetQueue(Queue, Queue.Count - 1);
                PlaySongAtIndex(Queue.Count - 1);
            }
        }

        public void PlaySongFromCollection(Song song, IEnumerable<Song> collection)
        {
            var songList = collection.ToList();
            var index = songList.FindIndex(s => s.Id == song.Id);

            if (index >= 0)
            {
                SetQueue(songList, index);
                PlaySongAtIndex(index);
            }
        }

        public void PlaySongAtIndex(int index, bool cameFromShuffle = false)
        {
            if (index < 0 || index >= Queue.Count)
                return;

            if (_isSwitchingTrack) return; // prevent re-entrancy
            _isSwitchingTrack = true;

            try
            {
                CurrentIndex = index;
                CurrentSong = Queue[index];

                if (IsShuffleOn && !cameFromShuffle)
                {
                    // User picked a song directly: align shuffle pointer to this song.
                    if (_shuffleOrder.Count == 0) GenerateShuffleOrder();
                    var pos = _shuffleOrder.FindIndex(i => i == index);
                    if (pos == -1)
                    {
                        GenerateShuffleOrder();
                        pos = _shuffleOrder.FindIndex(i => i == index);
                    }
                    _shuffleIndex = pos;
                }

                LoadFile(CurrentSong.FilePath);
                Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing song '{CurrentSong?.Title ?? "Unknown"}': {ex.Message}");
                MessageBox.Show($"Error playing '{CurrentSong?.Title ?? "Unknown"}': {ex.Message}",
                    "Playback Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Stop();
            }
            finally
            {
                _isSwitchingTrack = false;
            }
        }

        public void PlayNext()
        {
            if (Queue.Count == 0) return;

            if (IsShuffleOn)
            {
                PlayNextShuffle();
                return;
            }

            int next = CurrentIndex;

            if (RepeatMode == RepeatMode.RepeatOne)
            {
                // stay
            }
            else
            {
                next = CurrentIndex + 1;
                if (next >= Queue.Count)
                {
                    if (RepeatMode == RepeatMode.RepeatAll) next = 0;
                    else { Stop(); return; }
                }
            }

            PlaySongAtIndex(next);
        }


        public void PlayPrevious()
        {
            if (Queue.Count == 0) return;

            if (IsShuffleOn)
            {
                PlayPreviousShuffle();
                return;
            }

            int prev = CurrentIndex;

            if (RepeatMode == RepeatMode.RepeatOne)
            {
                // stay
            }
            else
            {
                prev = CurrentIndex - 1;
                if (prev < 0)
                {
                    if (RepeatMode == RepeatMode.RepeatAll) prev = Queue.Count - 1;
                    else { Stop(); return; }
                }
            }

            PlaySongAtIndex(prev);
        }

        private void PlayNextShuffle()
        {
            if (_shuffleOrder.Count == 0) GenerateShuffleOrder();
            if (_shuffleOrder.Count == 0) return;

            if (RepeatMode != RepeatMode.RepeatOne)
            {
                _shuffleIndex++;
                if (_shuffleIndex >= _shuffleOrder.Count)
                {
                    if (RepeatMode == RepeatMode.RepeatAll)
                    {
                        GenerateShuffleOrder();
                        _shuffleIndex = 0;
                    }
                    else { Stop(); return; }
                }
            }
            var idx = _shuffleOrder[_shuffleIndex];
            PlaySongAtIndex(idx, cameFromShuffle: true);
        }

        private void PlayPreviousShuffle()
        {
            if (_shuffleOrder.Count == 0) GenerateShuffleOrder();
            if (_shuffleOrder.Count == 0) return;

            if (RepeatMode != RepeatMode.RepeatOne)
            {
                _shuffleIndex--;
                if (_shuffleIndex < 0)
                {
                    if (RepeatMode == RepeatMode.RepeatAll)
                        _shuffleIndex = _shuffleOrder.Count - 1;
                    else { Stop(); return; }
                }
            }
            var idx = _shuffleOrder[_shuffleIndex];
            PlaySongAtIndex(idx, cameFromShuffle: true);
        }

        private int GetNextIndex()
        {
            if (Queue.Count == 0) return -1;

            // Spotify-like logic: Shuffle and Repeat are independent
            if (IsShuffleOn)
            {
                var nextShuffleIndex = GetNextShuffleIndex();
                if (nextShuffleIndex == -1 && RepeatMode == RepeatMode.RepeatAll)
                {
                    // Restart shuffle when repeat all is on
                    GenerateShuffleOrder();
                    _shuffleIndex = 0;
                    return _shuffleOrder.Count > 0 ? _shuffleOrder[0] : -1;
                }
                return nextShuffleIndex;
            }
            else
            {
                // Normal sequential play
                if (RepeatMode == RepeatMode.RepeatOne)
                {
                    return CurrentIndex; // Stay on same song
                }
                else if (RepeatMode == RepeatMode.RepeatAll)
                {
                    return (CurrentIndex + 1) % Queue.Count; // Loop back to start
                }
                else
                {
                    // Normal mode - stop at end
                    return CurrentIndex + 1 < Queue.Count ? CurrentIndex + 1 : -1;
                }
            }
        }

        private int GetPreviousIndex()
        {
            if (Queue.Count == 0) return -1;

            // Spotify-like logic: Shuffle and Repeat are independent
            if (IsShuffleOn)
            {
                var prevShuffleIndex = GetPreviousShuffleIndex();
                if (prevShuffleIndex == -1 && RepeatMode == RepeatMode.RepeatAll)
                {
                    // Go to end of shuffle when repeat all is on
                    if (_shuffleOrder.Count == 0) GenerateShuffleOrder();
                    _shuffleIndex = _shuffleOrder.Count - 1;
                    return _shuffleOrder.Count > 0 ? _shuffleOrder[_shuffleIndex] : -1;
                }
                return prevShuffleIndex;
            }
            else
            {
                // Normal sequential play
                if (RepeatMode == RepeatMode.RepeatOne)
                {
                    return CurrentIndex; // Stay on same song
                }
                else if (RepeatMode == RepeatMode.RepeatAll)
                {
                    return CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : Queue.Count - 1; // Loop to end
                }
                else
                {
                    // Normal mode - stop at beginning
                    return CurrentIndex - 1 >= 0 ? CurrentIndex - 1 : -1;
                }
            }
        }

        public void AddToQueue(Song song)
        {
            Queue.Add(song);

            if (IsShuffleOn)
            {
                _shuffleOrder.Add(Queue.Count - 1);
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

                if (IsShuffleOn)
                {
                    var shuffleOrderIndex = _shuffleOrder.FindIndex(i => i == index);
                    if (shuffleOrderIndex >= 0)
                    {
                        _shuffleOrder.RemoveAt(shuffleOrderIndex);
                        if (shuffleOrderIndex <= _shuffleIndex) _shuffleIndex--;
                    }

                    for (int i = 0; i < _shuffleOrder.Count; i++)
                    {
                        if (_shuffleOrder[i] > index) _shuffleOrder[i]--;
                    }
                }

                if (index < CurrentIndex)
                    CurrentIndex--;
                else if (index == CurrentIndex)
                {
                    if (Queue.Count > 0)
                        PlaySongAtIndex(Math.Min(CurrentIndex, Queue.Count - 1));
                    else
                        Stop();
                }
                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Shuffle Logic
        private void OnPlayModeChanged(PlayMode oldMode, PlayMode newMode)
        {
            if (newMode == PlayMode.Shuffle && oldMode != PlayMode.Shuffle)
            {
                GenerateShuffleOrder();
            }
            else if (oldMode == PlayMode.Shuffle && newMode != PlayMode.Shuffle)
            {
                _shuffleOrder.Clear();
                _shuffleIndex = -1;
            }
        }

        private void OnShuffleChanged(bool isShuffleOn)
        {
            if (isShuffleOn)
            {
                GenerateShuffleOrder();
            }
            else
            {
                _shuffleOrder.Clear();
                _shuffleIndex = -1;
            }
        }

        private void GenerateShuffleOrder()
        {
            if (Queue.Count == 0) return;

            _shuffleOrder.Clear();
            _shuffleOrder.AddRange(Enumerable.Range(0, Queue.Count));

            for (int i = _shuffleOrder.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
            }

            _shuffleIndex = _shuffleOrder.FindIndex(index => index == CurrentIndex);
            if (_shuffleIndex == -1 && CurrentIndex >= 0)
            {
                _shuffleOrder.Insert(0, CurrentIndex);
                _shuffleIndex = 0;
            }
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
                // End of shuffle queue - don't auto-repeat unless RepeatAll is on
                return -1;
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
                // Beginning of shuffle queue - don't auto-wrap unless RepeatAll is on
                return -1;
            }

            return _shuffleIndex >= 0 ? _shuffleOrder[_shuffleIndex] : -1;
        }
        #endregion

        #region Listening Statistics Methods
        /// <summary>
        /// Get weekend listening statistics (Saturday and Sunday of current week)
        /// </summary>
        public PeriodStatistics GetWeekendStats()
        {
            var stats = _listeningStatsService.GetWeekendStats();
            UpdateStatsWithSongMetadata(stats);
            return stats;
        }

        /// <summary>
        /// Get monthly listening statistics for the current month
        /// </summary>
        public PeriodStatistics GetMonthlyStats()
        {
            var stats = _listeningStatsService.GetMonthlyStats();
            UpdateStatsWithSongMetadata(stats);
            return stats;
        }

        /// <summary>
        /// Get all-time listening statistics
        /// </summary>
        public PeriodStatistics GetAllTimeStats()
        {
            var stats = _listeningStatsService.GetAllTimeStats();
            UpdateStatsWithSongMetadata(stats);
            return stats;
        }

        private void UpdateStatsWithSongMetadata(PeriodStatistics stats)
        {
            var songDict = Songs.ToDictionary(s => s.Id, s => s);

            foreach (var songStat in stats.TopSongs)
            {
                if (songDict.TryGetValue(songStat.SongId, out var song))
                {
                    songStat.SongTitle = song.Title;
                    songStat.Artist = song.Artist;
                    songStat.FilePath = song.FilePath;
                }
            }
        }
        #endregion

        #region Audio Separator/Karaoke Methods
        
        /// <summary>
        /// Switches the current song to karaoke mode (instrumental only)
        /// </summary>
        public async Task SwitchToKaraokeModeAsync()
        {
            if (CurrentSong == null) return;

            try
            {
                var stemsCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WpfMusicPlayer", "SeparatedStems");

                // Check if stems already exist
                if (!_audioSeparatorService.StemsExist(CurrentSong.FilePath, stemsCacheDir))
                {
                    // Separate the audio using the progress-enabled service
                    bool success = await _audioSeparatorService.SeparateAudioAsync(CurrentSong.FilePath, stemsCacheDir);
                    if (!success)
                    {
                        return; // Error reporting is handled by the progress events
                    }
                }

                // Get the separated file paths
                var (vocalsPath, accompanimentPath) = _audioSeparatorService.GetSeparatedFilePaths(CurrentSong.FilePath, stemsCacheDir);

                // Switch to the appropriate track based on karaoke mode
                string targetPath = IsKaraokeMode ? accompanimentPath : CurrentSong.FilePath;
                
                if (System.IO.File.Exists(targetPath))
                {
                    var currentPosition = CurrentPosition;
                    var wasPlaying = PlaybackState == Models.PlaybackState.Playing;

                    // Load the new audio file
                    LoadFile(targetPath);
                    _currentKaraokeFilePath = IsKaraokeMode ? targetPath : null;

                    // Restore position and playback state
                    CurrentPosition = currentPosition;
                    if (wasPlaying)
                    {
                        Play();
                    }
                }
            }
            catch (Exception ex)
            {
                // Report error through progress event
                AudioSeparationProgress?.Invoke(this, new AudioSeparatorService.ProgressEventArgs
                {
                    Message = $"Error switching to karaoke mode: {ex.Message}",
                    IsCompleted = true,
                    IsError = true
                });
            }
        }

        /// <summary>
        /// Exports the instrumental version of a song
        /// </summary>
        /// <param name="song">The song to export</param>
        /// <param name="outputPath">Where to save the instrumental file</param>
        public async Task<bool> ExportInstrumentalAsync(Song song, string outputPath)
        {
            try
            {
                var stemsCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WpfMusicPlayer", "SeparatedStems");

                // Check if stems already exist
                if (!_audioSeparatorService.StemsExist(song.FilePath, stemsCacheDir))
                {
                    // Separate the audio
                    bool success = await _audioSeparatorService.SeparateAudioAsync(song.FilePath, stemsCacheDir);
                    if (!success)
                    {
                        return false;
                    }
                }

                // Get the separated file paths
                var (vocalsPath, accompanimentPath) = _audioSeparatorService.GetSeparatedFilePaths(song.FilePath, stemsCacheDir);

                // Copy the accompaniment (instrumental) file to the output path
                if (System.IO.File.Exists(accompanimentPath))
                {
                    System.IO.File.Copy(accompanimentPath, outputPath, true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting instrumental: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Exports the vocals-only version of a song
        /// </summary>
        /// <param name="song">The song to export</param>
        /// <param name="outputPath">Where to save the vocals file</param>
        public async Task<bool> ExportVocalsAsync(Song song, string outputPath)
        {
            try
            {
                var stemsCacheDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WpfMusicPlayer", "SeparatedStems");

                // Check if stems already exist
                if (!_audioSeparatorService.StemsExist(song.FilePath, stemsCacheDir))
                {
                    // Separate the audio
                    bool success = await _audioSeparatorService.SeparateAudioAsync(song.FilePath, stemsCacheDir);
                    if (!success)
                    {
                        return false;
                    }
                }

                // Get the separated file paths
                var (vocalsPath, accompanimentPath) = _audioSeparatorService.GetSeparatedFilePaths(song.FilePath, stemsCacheDir);

                // Copy the vocals file to the output path
                if (System.IO.File.Exists(vocalsPath))
                {
                    System.IO.File.Copy(vocalsPath, outputPath, true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting vocals: {ex.Message}", 
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Pre-processes songs for faster karaoke switching by separating them in advance
        /// </summary>
        /// <param name="songs">Songs to pre-process</param>
        public async Task PreprocessSongsForKaraokeAsync(IEnumerable<Song> songs)
        {
            var stemsCacheDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WpfMusicPlayer", "SeparatedStems");

            foreach (var song in songs)
            {
                if (!_audioSeparatorService.StemsExist(song.FilePath, stemsCacheDir))
                {
                    await _audioSeparatorService.SeparateAudioAsync(song.FilePath, stemsCacheDir);
                }
            }
        }

        #endregion

        #region Event Handlers
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            // Ignore events from an old player (stale Stop after we already switched)
            if (!ReferenceEquals(sender, _wavePlayer))
                return;

            _positionTimer.Stop();
            OnPropertyChanged(nameof(PlaybackState));

            if (e.Exception == null && !_isLoadingNewSong)
            {
                // Use the centralized logic to advance
                PlayNext();
            }

            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }


        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Stop tracking current song before disposing
                _listeningStatsService?.OnSongStopped();
                
                _positionTimer?.Stop();
                StopPlayback();
                _disposed = true;
            }
        }
        #endregion
    }
}
