using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer
{
    public partial class PlaylistDetailsWindow : Window
    {
        private readonly Playlist _playlist;
        private readonly MusicLibraryService _musicLibrary;
        private readonly PlayerService _playerService;
        private readonly ObservableCollection<Song> _filteredSongs;

        public PlaylistDetailsWindow(Playlist playlist, MusicLibraryService musicLibrary, PlayerService playerService)
        {
            InitializeComponent();
            
            _playlist = playlist;
            _musicLibrary = musicLibrary;
            _playerService = playerService;
            _filteredSongs = new ObservableCollection<Song>();

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            DataContext = _playlist;
            PlaylistSongsListView.ItemsSource = _filteredSongs;
            
            RefreshSongsList();
            
            SearchTextBox.GotFocus += (s, e) =>
            {
                if (SearchTextBox.Text == "Search in playlist...")
                    SearchTextBox.Text = "";
            };

            SearchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    SearchTextBox.Text = "Search in playlist...";
            };
        }

        private void RefreshSongsList()
        {
            _filteredSongs.Clear();
            foreach (var song in _playlist.Songs)
            {
                _filteredSongs.Add(song);
            }
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            StatusText.Text = $"{_filteredSongs.Count} songs displayed";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Search in playlist...")
                return;

            _filteredSongs.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                foreach (var song in _playlist.Songs)
                    _filteredSongs.Add(song);
            }
            else
            {
                var query = SearchTextBox.Text.ToLowerInvariant();
                foreach (var song in _playlist.Songs.Where(s =>
                    s.Title.ToLowerInvariant().Contains(query) ||
                    s.Artist.ToLowerInvariant().Contains(query) ||
                    s.Album.ToLowerInvariant().Contains(query)))
                {
                    _filteredSongs.Add(song);
                }
            }
            
            UpdateStatusText();
        }

        private void PlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (_filteredSongs.Count > 0)
            {
                _playerService.SetQueue(_filteredSongs, 0);
                _playerService.PlaySongAtIndex(0);
                StatusText.Text = "Playing playlist...";
            }
        }

        private void EditPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PlaylistNameDialog
            {
                PlaylistName = _playlist.Name,
                PlaylistDescription = _playlist.Description
            };
            
            if (dialog.ShowDialog() == true)
            {
                _playlist.Name = dialog.PlaylistName;
                _playlist.Description = dialog.PlaylistDescription;
                if (dialog.PlaylistCover != null)
                {
                    _playlist.CoverImage = dialog.PlaylistCover;
                }
                
                // Save the playlist changes
                _musicLibrary.UpdatePlaylist(_playlist);
                
                // Refresh the display
                PlaylistNameText.Text = _playlist.Name;
                PlaylistDescriptionText.Text = _playlist.Description;
                Title = $"Playlist: {_playlist.Name}";
                StatusText.Text = "Playlist updated";
            }
        }

        private void AddSongsButton_Click(object sender, RoutedEventArgs e)
        {
            var songSelectionDialog = new SongSelectionDialog(_musicLibrary.Songs, _playlist.Songs);
            if (songSelectionDialog.ShowDialog() == true)
            {
                foreach (var song in songSelectionDialog.SelectedSongs)
                {
                    _musicLibrary.AddSongToPlaylist(_playlist, song);
                }
                RefreshSongsList();
                StatusText.Text = $"Added {songSelectionDialog.SelectedSongs.Count} songs to playlist";
            }
        }

        private void RemoveFromPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Song song)
            {
                var result = MessageBox.Show($"Remove '{song.Title}' from this playlist?", 
                    "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    _musicLibrary.RemoveSongFromPlaylist(_playlist, song);
                    RefreshSongsList();
                    StatusText.Text = "Song removed from playlist";
                }
            }
        }

        private void PlaylistSongsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PlaylistSongsListView.SelectedItem is Song song)
            {
                // Play song from the filtered playlist collection
                _playerService.PlaySongFromCollection(song, _filteredSongs);
                StatusText.Text = $"Now playing: {song.Title}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
