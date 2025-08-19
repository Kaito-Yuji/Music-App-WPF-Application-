using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer
{
    public partial class SongSelectionDialog : Window
    {
        private readonly ObservableCollection<Song> _filteredSongs;
        private readonly IEnumerable<Song> _playlistSongs;
        
        public List<Song> SelectedSongs { get; private set; } = new List<Song>();

        public SongSelectionDialog(IEnumerable<Song> allSongs, IEnumerable<Song> playlistSongs)
        {
            InitializeComponent();
            
            _filteredSongs = new ObservableCollection<Song>();
            _playlistSongs = playlistSongs;
            
            SongsListView.ItemsSource = _filteredSongs;
            SongsListView.SelectionChanged += SongsListView_SelectionChanged;

            // Sort by ViewCount desc, then Title asc
            var view = CollectionViewSource.GetDefaultView(SongsListView.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(nameof(Song.ViewCount), ListSortDirection.Descending));
            view.SortDescriptions.Add(new SortDescription(nameof(Song.Title), ListSortDirection.Ascending));
            
            LoadSongs(allSongs);
            InitializeSearch();
        }

        private void LoadSongs(IEnumerable<Song> songs)
        {
            _filteredSongs.Clear();
            
            foreach (var song in songs)
            {
                if (!_playlistSongs.Contains(song))
                {
                    _filteredSongs.Add(song);
                }
            }
            
            UpdateStatusText();
        }

        private void InitializeSearch()
        {
            SearchTextBox.GotFocus += (s, e) =>
            {
                if (SearchTextBox.Text == "Search songs...")
                    SearchTextBox.Text = "";
            };

            SearchTextBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
                    SearchTextBox.Text = "Search songs...";
            };
        }

        private void UpdateStatusText()
        {
            var selectedCount = SongsListView.SelectedItems.Count;
            StatusTextBlock.Text = $"{selectedCount} songs selected ({_filteredSongs.Count} available)";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (SearchTextBox.Text == "Search songs...")
                return;

            var currentSongs = _filteredSongs.ToList(); // Store current filtered songs
            _filteredSongs.Clear();
            
            if (string.IsNullOrWhiteSpace(SearchTextBox.Text))
            {
                // Show all available songs (not already in playlist)
                foreach (var song in currentSongs)
                {
                    _filteredSongs.Add(song);
                }
            }
            else
            {
                var query = SearchTextBox.Text.ToLowerInvariant();
                var filteredResults = currentSongs.Where(s =>
                    s.Title.ToLowerInvariant().Contains(query) ||
                    s.Artist.ToLowerInvariant().Contains(query) ||
                    s.Album.ToLowerInvariant().Contains(query));
                
                foreach (var song in filteredResults)
                {
                    _filteredSongs.Add(song);
                }
            }
            
            UpdateStatusText();
        }

        private void SongsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatusText();
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            SongsListView.SelectAll();
            UpdateStatusText();
        }

        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            SongsListView.UnselectAll();
            UpdateStatusText();
        }

        private void AddSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedSongs = SongsListView.SelectedItems.Cast<Song>().ToList();
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
