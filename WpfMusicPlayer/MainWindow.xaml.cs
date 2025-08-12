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

namespace WpfMusicPlayer
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly MusicLibraryService _musicLibrary;
        private readonly PlayerService _playerService;
        private DispatcherTimer? _uiUpdateTimer;
        private bool _isDraggingSlider = false;

        public ObservableCollection<Song> DisplayedSongs { get; } = new ObservableCollection<Song>();

        public MainWindow()
        {
            InitializeComponent();
            
            _musicLibrary = new MusicLibraryService();
            _playerService = new PlayerService();

            InitializeServices();
            InitializeUI();
            SetupTimers();
            LoadSavedMusicFolder();
        }

        private void InitializeServices()
        {
            _musicLibrary.ScanProgressChanged += (s, message) => 
            {
                Dispatcher.Invoke(() => StatusTextBlock.Text = message);
            };

            _playerService.CurrentSongChanged += OnCurrentSongChanged;
            _playerService.PlaybackStateChanged += OnPlaybackStateChanged;
            _playerService.PositionChanged += OnPositionChanged;
            _playerService.QueueChanged += OnQueueChanged;
        }

        private void InitializeUI()
        {
            DataContext = _playerService;
            SongsListView.ItemsSource = DisplayedSongs;
            PlaylistsListBox.ItemsSource = _musicLibrary.Playlists;
            QueueListBox.ItemsSource = _playerService.Queue;

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
                await _musicLibrary.ScanFolderAsync(savedPath);
                UpdateDisplayedSongs(_musicLibrary.Songs);
                StatusTextBlock.Text = $"Found {_musicLibrary.Songs.Count} songs from saved folder";
            }
        }

        private void UpdateUI(object? sender, EventArgs e)
        {
            if (!_isDraggingSlider)
            {
                var position = _playerService.CurrentPosition;
                var total = _playerService.TotalDuration;

                CurrentTimeText.Text = position.ToString(@"m\:ss");
                TotalTimeText.Text = total.ToString(@"m\:ss");

                if (total.TotalSeconds > 0)
                    ProgressSlider.Value = position.TotalSeconds;
            }

            // Update play/pause button
            PlayPauseButton.Content = _playerService.PlaybackState == PlaybackState.Playing ? "⏸️" : "▶️";
        }

        #region Event Handlers

        private void OnCurrentSongChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_playerService.CurrentSong));
        }

        private void OnPlaybackStateChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_playerService.PlaybackState));
        }

        private void OnPositionChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_playerService.CurrentPosition));
        }

        private void OnQueueChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(_playerService.Queue));
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
                await _musicLibrary.ScanFolderAsync(dialog.SelectedPath);
                UpdateDisplayedSongs(_musicLibrary.Songs);
                StatusTextBlock.Text = $"Found {_musicLibrary.Songs.Count} songs";
                
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
                _musicLibrary.CreatePlaylist(dialog.PlaylistName, dialog.PlaylistDescription, dialog.PlaylistCover);
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
                    _musicLibrary.DeletePlaylist(playlist);
                }
            }
        }

        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                var dialog = new PlaylistSelectionDialog(_musicLibrary.Playlists);
                if (dialog.ShowDialog() == true && dialog.SelectedPlaylist != null)
                {
                    _musicLibrary.AddSongToPlaylist(dialog.SelectedPlaylist, song);
                }
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_playerService.PlaybackState == PlaybackState.Playing)
            {
                _playerService.Pause();
            }
            else
            {
                // If no song is currently playing, play the selected song
                if (_playerService.CurrentSong == null && SongsListView.SelectedItem is Song selectedSong)
                {
                    _playerService.PlaySongDirectly(selectedSong);
                }
                else
                {
                    _playerService.Play();
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _playerService.Stop();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // UI-driven approach - update selection first, then play
            var currentIndex = SongsListView.SelectedIndex;
            if (currentIndex < DisplayedSongs.Count - 1)
            {
                // Move to next song in the list
                SongsListView.SelectedIndex = currentIndex + 1;
                
                // Play the newly selected song
                if (SongsListView.SelectedItem is Song nextSong)
                {
                    _playerService.PlaySongDirectly(nextSong);
                }
            }
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // UI-driven approach - update selection first, then play
            var currentIndex = SongsListView.SelectedIndex;
            if (currentIndex > 0)
            {
                // Move to previous song in the list
                SongsListView.SelectedIndex = currentIndex - 1;
                
                // Play the newly selected song
                if (SongsListView.SelectedItem is Song prevSong)
                {
                    _playerService.PlaySongDirectly(prevSong);
                }
            }
        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            _playerService.PlayMode = _playerService.PlayMode switch
            {
                PlayMode.Normal => PlayMode.RepeatAll,
                PlayMode.RepeatAll => PlayMode.RepeatOne,
                PlayMode.RepeatOne => PlayMode.Shuffle,
                PlayMode.Shuffle => PlayMode.Normal,
                _ => PlayMode.Normal
            };
            OnPropertyChanged(nameof(_playerService.PlayMode));
        }

        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                _playerService.RemoveFromQueue(song);
            }
        }

        #endregion

        #region UI Event Handlers

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Search songs, artists, albums...")
                return;

            var results = _musicLibrary.SearchSongs(SearchTextBox.Text);
            UpdateDisplayedSongs(results);
            StatusTextBlock.Text = $"Found {results.Count} songs";
        }

        private void SongsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SongsListView.SelectedItem is Song song)
            {
                // Simple direct playback - no complex queue bullshit
                _playerService.PlaySongDirectly(song);
            }
        }

        private void QueueListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (QueueListBox.SelectedItem is Song song)
            {
                // Simple direct playback for queue items too
                _playerService.PlaySongDirectly(song);
            }
        }

        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is Playlist playlist)
            {
                var playlistWindow = new PlaylistDetailsWindow(playlist, _musicLibrary, _playerService);
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
            _playerService.Seek(newPosition);
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_playerService != null)
                _playerService.Volume = (float)e.NewValue;
        }

        #endregion

        #region Helper Methods

        private void UpdateDisplayedSongs(IEnumerable<Song> songs)
        {
            DisplayedSongs.Clear();
            foreach (var song in songs)
                DisplayedSongs.Add(song);
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
            _playerService?.Dispose();
            base.OnClosed(e);
        }

        private void SongsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}