using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer
{
    public partial class SongSelectionDialog : Window
    {
        /// <summary>
        /// Every song that may be added. Searching always filters this list rather than the
        /// previous result, so clearing the box brings the full set back.
        /// </summary>
        private readonly List<Song> _availableSongs;

        public SongSelectionDialog(IEnumerable<Song> allSongs, IEnumerable<Song> alreadyIncluded)
        {
            InitializeComponent();

            var excluded = new HashSet<Song>(alreadyIncluded);
            _availableSongs = allSongs.Where(song => !excluded.Contains(song)).ToList();

            SongsListView.ItemsSource = FilteredSongs;
            SongsListView.SelectionChanged += (_, _) => UpdateStatusText();

            ApplyFilter();
        }

        public ObservableCollection<Song> FilteredSongs { get; } = new();

        public List<Song> SelectedSongs { get; private set; } = new();

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = SearchTextBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;

            var matches = string.IsNullOrEmpty(query)
                ? _availableSongs
                : _availableSongs.Where(s =>
                    s.Title.ToLowerInvariant().Contains(query) ||
                    s.Artist.ToLowerInvariant().Contains(query) ||
                    s.Album.ToLowerInvariant().Contains(query)).ToList();

            FilteredSongs.Clear();
            foreach (var song in matches)
                FilteredSongs.Add(song);

            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (StatusTextBlock == null)
                return;

            StatusTextBlock.Text =
                $"{SongsListView.SelectedItems.Count} songs selected ({FilteredSongs.Count} available)";
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
