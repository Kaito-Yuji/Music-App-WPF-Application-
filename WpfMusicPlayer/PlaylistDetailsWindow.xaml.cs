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
        private readonly MusicService _musicService;
        private readonly ObservableCollection<Song> _filteredSongs;

        public PlaylistDetailsWindow(Playlist playlist, MusicService musicService)
        {
            InitializeComponent();
            
            _playlist = playlist;
            _musicService = musicService;
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

            // Add responsive column sizing
            SizeChanged += PlaylistDetailsWindow_SizeChanged;
            Loaded += PlaylistDetailsWindow_Loaded;
        }

        private void PlaylistDetailsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateColumnWidths();
        }

        private void PlaylistDetailsWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateColumnWidths();
        }

        private void UpdateColumnWidths()
        {
            if (PlaylistSongsListView?.View is GridView gridView && gridView.Columns.Count >= 8)
            {
                var availableWidth = PlaylistSongsListView.ActualWidth - 40; // Account for scrollbar and padding
                
                if (availableWidth > 0)
                {
                    // Fixed width columns
                    var numberWidth = 40;
                    var coverWidth = 80;
                    var durationWidth = 80;
                    var viewsWidth = 70;
                    var actionsWidth = 80;
                    
                    // Calculate remaining width for flexible columns
                    var remainingWidth = availableWidth - numberWidth - coverWidth - durationWidth - viewsWidth - actionsWidth;
                    
                    if (remainingWidth > 300) // Ensure minimum width for text columns
                    {
                        // Distribute remaining width: Title gets 40%, Artist and Album get 30% each
                        var titleWidth = Math.Max(150, remainingWidth * 0.4);
                        var artistWidth = Math.Max(120, remainingWidth * 0.3);
                        var albumWidth = Math.Max(120, remainingWidth * 0.3);
                        
                        // Set the widths
                        gridView.Columns[0].Width = numberWidth;   // #
                        gridView.Columns[1].Width = coverWidth;   // Cover
                        gridView.Columns[2].Width = titleWidth;   // Title
                        gridView.Columns[3].Width = artistWidth;  // Artist
                        gridView.Columns[4].Width = albumWidth;   // Album
                        gridView.Columns[5].Width = durationWidth; // Duration
                        gridView.Columns[6].Width = viewsWidth;   // Views
                        gridView.Columns[7].Width = actionsWidth; // Actions
                    }
                }
            }
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
                _musicService.SetQueue(_filteredSongs, 0);
                _musicService.PlaySongAtIndex(0);
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
                _musicService.UpdatePlaylist(_playlist);
                
                // Refresh the display
                PlaylistNameText.Text = _playlist.Name;
                PlaylistDescriptionText.Text = _playlist.Description;
                Title = $"Playlist: {_playlist.Name}";
                StatusText.Text = "Playlist updated";
            }
        }

        private void AddSongsButton_Click(object sender, RoutedEventArgs e)
        {
            var songSelectionDialog = new SongSelectionDialog(_musicService.Songs, _playlist.Songs);
            if (songSelectionDialog.ShowDialog() == true)
            {
                foreach (var song in songSelectionDialog.SelectedSongs)
                {
                    _musicService.AddSongToPlaylist(_playlist, song);
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
                    _musicService.RemoveSongFromPlaylist(_playlist, song);
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
                _musicService.PlaySongFromCollection(song, _filteredSongs);
                StatusText.Text = $"Now playing: {song.Title}";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
