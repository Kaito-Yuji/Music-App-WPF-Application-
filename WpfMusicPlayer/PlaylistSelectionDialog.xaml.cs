using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer
{
    public partial class PlaylistSelectionDialog : Window
    {
        public PlaylistSelectionDialog(IEnumerable<Playlist> playlists)
        {
            InitializeComponent();
            PlaylistsListBox.ItemsSource = playlists.ToList();
        }

        public Playlist? SelectedPlaylist { get; private set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is not Playlist playlist)
            {
                MessageBox.Show(this, "Please select a playlist.", "No Playlist Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedPlaylist = playlist;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void PlaylistsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PlaylistsListBox.SelectedItem is Playlist)
                OkButton_Click(sender, e);
        }
    }
}
