using System.Collections.ObjectModel;
using System.Windows;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer
{
    public partial class PlaylistSelectionDialog : Window
    {
        public Playlist? SelectedPlaylist { get; private set; }

        public PlaylistSelectionDialog(ObservableCollection<Playlist> playlists)
        {
            InitializeComponent();
            PlaylistsListBox.ItemsSource = playlists;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is Playlist playlist)
            {
                SelectedPlaylist = playlist;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select a playlist.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PlaylistsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is Playlist)
            {
                OkButton_Click(sender, e);
            }
        }
    }
}
