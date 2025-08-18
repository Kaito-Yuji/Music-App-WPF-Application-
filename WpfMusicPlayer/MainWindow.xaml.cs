using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;
using WpfMusicPlayer.Properties;
using System.IO;
using System.Linq;

namespace WpfMusicPlayer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly MusicService _musicService;
        private DispatcherTimer? _uiUpdateTimer;
        private bool _isDraggingSlider = false;

        public ObservableCollection<Song> DisplayedSongs { get; } = new ObservableCollection<Song>();

        public MainWindow()
        {
            InitializeComponent();
            
            _musicService = new MusicService();

            InitializeServices();
            InitializeUI();
            SetupTimers();
            LoadSavedMusicFolder();
        }

        private void InitializeServices()
        {
            _musicService.ScanProgressChanged += (s, message) => 
            {
                Dispatcher.Invoke(() => StatusTextBlock.Text = message);
            };

            _musicService.CurrentSongChanged += OnCurrentSongChanged;
            _musicService.PlaybackStateChanged += OnPlaybackStateChanged;
            _musicService.PositionChanged += OnPositionChanged;
            _musicService.QueueChanged += OnQueueChanged;
        }

        private void InitializeUI()
        {
            DataContext = _musicService;
            SongsListView.ItemsSource = DisplayedSongs;
            PlaylistsListBox.ItemsSource = _musicService.Playlists;
            QueueListBox.ItemsSource = _musicService.Queue;

            // Set initial search placeholder behavior
            SearchTextBox.GotFocus += (s, e) =>
            {
                if (SearchTextBox.Text == "Search songs, artists, albums...")
                    SearchTextBox.Text = "";
            };

            SearchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    SearchTextBox.Text = "Search songs, artists, albums...";
            };

            // Add size changed event for responsive columns
            SizeChanged += MainWindow_SizeChanged;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColumnWidths();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnWidths();
        }

        private void UpdateColumnWidths()
        {
            if (SongsListView?.View is GridView gridView && gridView.Columns.Count >= 7)
            {
                var availableWidth = SongsListView.ActualWidth - 40; // Account for scrollbar and padding
                
                if (availableWidth > 0)
                {
                    // Fixed width columns
                    var numberWidth = 40;
                    var coverWidth = 80;
                    var durationWidth = 80;
                    var actionsWidth = 150;
                    
                    // Calculate remaining width for flexible columns
                    var remainingWidth = availableWidth - numberWidth - coverWidth - durationWidth - actionsWidth;
                    
                    if (remainingWidth > 300) // Ensure minimum width for text columns
                    {
                        // Distribute remaining width: Title gets 40%, Artist and Album get 30% each
                        var titleWidth = Math.Max(150, remainingWidth * 0.4);
                        var artistWidth = Math.Max(120, remainingWidth * 0.3);
                        var albumWidth = Math.Max(120, remainingWidth * 0.3);
                        
                        // Set the widths
                        gridView.Columns[0].Width = numberWidth;    // #
                        gridView.Columns[1].Width = coverWidth;    // Cover
                        gridView.Columns[2].Width = titleWidth;    // Title
                        gridView.Columns[3].Width = artistWidth;   // Artist
                        gridView.Columns[4].Width = albumWidth;    // Album
                        gridView.Columns[5].Width = durationWidth; // Duration
                        gridView.Columns[6].Width = actionsWidth;  // Actions
                    }
                }
            }
        }

        private void SetupTimers()
        {
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _uiUpdateTimer.Tick += UpdateUI;
            _uiUpdateTimer.Start();
        }

        private async void LoadSavedMusicFolder()
        {
            var savedPath = Settings.Default.MusicFolderPath;
            if (!string.IsNullOrWhiteSpace(savedPath) && Directory.Exists(savedPath))
            {
                StatusTextBlock.Text = "Loading saved music library...";
                await _musicService.ScanFolderAsync(savedPath);
                UpdateDisplayedSongs(_musicService.Songs);
                StatusTextBlock.Text = $"Found {_musicService.Songs.Count} songs from saved folder";
            }
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            if (!_isDraggingSlider)
            {
                var position = _musicService.CurrentPosition;
                var total = _musicService.TotalDuration;

                CurrentTimeText.Text = position.ToString(@"m\:ss");
                TotalTimeText.Text = total.ToString(@"m\:ss");

                // Only update slider if we have a valid total duration and position is within bounds
                if (total.TotalSeconds > 0 && position.TotalSeconds <= total.TotalSeconds)
                {
                    ProgressSlider.Value = position.TotalSeconds;
                }
                else if (total.TotalSeconds > 0)
                {
                    // Reset to start if position is out of bounds
                    ProgressSlider.Value = 0;
                }
            }

            // Update play/pause button
            PlayPauseButton.Content = _musicService.PlaybackState == PlaybackState.Playing ? "⏸️" : "▶️";
        }

        #region Event Handlers

        private void OnCurrentSongChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_musicService.CurrentSong));
            
            // Update UI selection to reflect the current song
            UpdateUISelection();
        }

        private void OnPlaybackStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_musicService.PlaybackState));
        }

        private void OnPositionChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_musicService.CurrentPosition));
        }

        private void OnQueueChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_musicService.Queue));
        }

        #endregion

        #region Button Click Handlers

        private async void ScanMusicButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                Description = "Select Music Folder"
            };

            if (dialog.ShowDialog(this) == true)
            {
                StatusTextBlock.Text = "Scanning...";
                await _musicService.ScanFolderAsync(dialog.SelectedPath);
                UpdateDisplayedSongs(_musicService.Songs);
                StatusTextBlock.Text = $"Found {_musicService.Songs.Count} songs";
                
                // Save the selected folder path
                Settings.Default.MusicFolderPath = dialog.SelectedPath;
                Settings.Default.Save();
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PlaylistNameDialog();
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.PlaylistName))
            {
                _musicService.CreatePlaylist(dialog.PlaylistName, dialog.PlaylistDescription, dialog.PlaylistCover);
            }
        }

        private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Playlist playlist)
            {
                var result = MessageBox.Show($"Delete playlist '{playlist.Name}'?", "Confirm Delete", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _musicService.DeletePlaylist(playlist);
                }
            }
        }

        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                var dialog = new PlaylistSelectionDialog(_musicService.Playlists);
                if (dialog.ShowDialog() == true && dialog.SelectedPlaylist != null)
                {
                    _musicService.AddSongToPlaylist(dialog.SelectedPlaylist, song);
                }
            }
        }

        private void EditSongButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                var dialog = new SongEditDialog(song);
                dialog.Owner = this;
                
                if (dialog.ShowDialog() == true && dialog.WasModified)
                {
                    // Update the song with the edited information
                    var editedSong = dialog.EditedSong;
                    
                    // Update the original song object
                    song.Title = editedSong.Title;
                    song.Artist = editedSong.Artist;
                    song.Album = editedSong.Album;
                    song.Genre = editedSong.Genre;
                    song.Year = editedSong.Year;
                    song.AlbumArt = editedSong.AlbumArt;

                    // Save the changes to the file's metadata if possible
                    try
                    {
                        _musicService.UpdateSongMetadata(song);
                        StatusTextBlock.Text = $"Updated metadata for '{song.Title}'";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Song information updated in the app, but failed to save to file: {ex.Message}", 
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        StatusTextBlock.Text = $"Updated '{song.Title}' (app only)";
                    }

                    // Refresh the UI to show the updated information
                    RefreshSongDisplay();
                    
                    // If this is the currently playing song, update the now playing display
                    if (_musicService.CurrentSong?.Id == song.Id)
                    {
                        OnPropertyChanged(nameof(_musicService.CurrentSong));
                    }
                }
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_musicService.PlaybackState == PlaybackState.Playing)
            {
                _musicService.Pause();
            }
            else
            {
                // If no song is currently playing, play the selected song
                if (_musicService.CurrentSong == null && SongsListView.SelectedItem is Song selectedSong)
                {
                    _musicService.PlaySongFromCollection(selectedSong, DisplayedSongs);
                }
                else
                {
                    _musicService.Play();
                }
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // If no queue, use the displayed songs as queue
            if (_musicService.Queue.Count == 0 && DisplayedSongs.Count > 0)
            {
                // Smart queue initialization - start from current song position if available
                int startIndex = 0;
                if (_musicService.CurrentSong != null)
                {
                    var currentInDisplay = DisplayedSongs.ToList().FindIndex(s => s.Id == _musicService.CurrentSong.Id);
                    if (currentInDisplay >= 0)
                        startIndex = currentInDisplay;
                }
                _musicService.SetQueue(DisplayedSongs, startIndex);
            }
            
            // Use MusicService's proper next logic that respects play modes
            _musicService.PlayNext();
            
            // Update UI selection to reflect the new current song
            UpdateUISelection();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // If no queue, use the displayed songs as queue
            if (_musicService.Queue.Count == 0 && DisplayedSongs.Count > 0)
            {
                // Smart queue initialization - start from current song position if available
                int startIndex = 0;
                if (_musicService.CurrentSong != null)
                {
                    var currentInDisplay = DisplayedSongs.ToList().FindIndex(s => s.Id == _musicService.CurrentSong.Id);
                    if (currentInDisplay >= 0)
                        startIndex = currentInDisplay;
                }
                _musicService.SetQueue(DisplayedSongs, startIndex);
            }
            
            // Use MusicService's proper previous logic that respects play modes
            _musicService.PlayPrevious();
            
            // Update UI selection to reflect the new current song
            UpdateUISelection();
        }
        
        private void UpdateUISelection()
        {
            if (_musicService.CurrentSong != null)
            {
                var songInDisplay = DisplayedSongs.FirstOrDefault(s => s.Id == _musicService.CurrentSong.Id);
                if (songInDisplay != null)
                {
                    SongsListView.SelectedItem = songInDisplay;
                    SongsListView.ScrollIntoView(songInDisplay);
                }
            }
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle shuffle on/off like Spotify
            _musicService.IsShuffleOn = !_musicService.IsShuffleOn;
            OnPropertyChanged(nameof(_musicService.IsShuffleOn));
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            // Cycle through repeat modes like Spotify: Off -> RepeatAll -> RepeatOne -> Off
            _musicService.RepeatMode = _musicService.RepeatMode switch
            {
                RepeatMode.Off => RepeatMode.RepeatAll,
                RepeatMode.RepeatAll => RepeatMode.RepeatOne,
                RepeatMode.RepeatOne => RepeatMode.Off,
                _ => RepeatMode.Off
            };
            OnPropertyChanged(nameof(_musicService.RepeatMode));
        }

        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                _musicService.RemoveFromQueue(song);
            }
        }

        #endregion

        #region UI Event Handlers

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Search songs, artists, albums...")
                return;

            var results = _musicService.SearchSongs(SearchTextBox.Text);
            UpdateDisplayedSongs(results);
            StatusTextBlock.Text = $"Found {results.Count} songs";
        }

        private void SongsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongsListView.SelectedItem is Song song)
            {
                // Set the current displayed songs as queue and play the selected song
                _musicService.PlaySongFromCollection(song, DisplayedSongs);
            }
        }

        private void QueueListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (QueueListBox.SelectedItem is Song song)
            {
                // Simple direct playback for queue items too
                _musicService.PlaySong(song);
            }
        }

        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is Playlist playlist)
            {
                var playlistWindow = new PlaylistDetailsWindow(playlist, _musicService);
                playlistWindow.Owner = this;
                playlistWindow.ShowDialog();
                
                // Clear selection after closing dialog
                PlaylistsListBox.SelectedItem = null;
            }
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;
            var newPosition = TimeSpan.FromSeconds(ProgressSlider.Value);
            _musicService.Seek(newPosition);
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
            
            // Handle clicking on the track to seek to that position
            if (sender is Slider slider)
            {
                var position = e.GetPosition(slider);
                var percentage = position.X / slider.ActualWidth;
                var newValue = slider.Minimum + (slider.Maximum - slider.Minimum) * percentage;
                
                // Clamp the value within bounds
                newValue = Math.Max(slider.Minimum, Math.Min(slider.Maximum, newValue));
                slider.Value = newValue;
                
                var newPosition = TimeSpan.FromSeconds(newValue);
                _musicService.Seek(newPosition);
            }
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Update the current time display when dragging
            if (_isDraggingSlider)
            {
                CurrentTimeText.Text = TimeSpan.FromSeconds(e.NewValue).ToString(@"m\:ss");
            }
        }

        private void ProgressSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void ProgressSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            _isDraggingSlider = false;
            if (sender is Slider slider)
            {
                var newPosition = TimeSpan.FromSeconds(slider.Value);
                _musicService.Seek(newPosition);
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_musicService != null)
                _musicService.Volume = (float)e.NewValue;
        }

        #endregion

        #region Helper Methods

        private void UpdateDisplayedSongs(IEnumerable<Song> songs)
        {
            DisplayedSongs.Clear();
            foreach (var song in songs)
                DisplayedSongs.Add(song);
        }

        private void RefreshSongDisplay()
        {
            // Force refresh of the ListView by updating the collection
            var currentSongs = DisplayedSongs.ToList();
            UpdateDisplayedSongs(currentSongs);
            
            // Refresh current search results if search is active
            if (SearchTextBox.Text != "Search songs, artists, albums..." && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                var results = _musicService.SearchSongs(SearchTextBox.Text);
                UpdateDisplayedSongs(results);
                StatusTextBlock.Text = $"Found {results.Count} songs";
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _uiUpdateTimer?.Stop();
            _musicService?.Dispose();
            base.OnClosed(e);
        }

        private void SongsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}