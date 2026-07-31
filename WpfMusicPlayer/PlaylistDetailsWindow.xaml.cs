using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer
{
    public partial class PlaylistDetailsWindow : Window
    {
        private readonly PlaylistDetailsViewModel _viewModel;

        public PlaylistDetailsWindow(PlaylistDetailsViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = viewModel;

            Loaded += (_, _) => UpdateColumnWidths();
            SizeChanged += (_, _) => UpdateColumnWidths();
        }

        private void UpdateColumnWidths()
        {
            if (PlaylistSongsListView?.View is not GridView gridView || gridView.Columns.Count < 8)
                return;

            var availableWidth = PlaylistSongsListView.ActualWidth - 40;
            if (availableWidth <= 0)
                return;

            const double numberWidth = 40;
            const double coverWidth = 80;
            const double durationWidth = 80;
            const double playsWidth = 70;
            const double actionsWidth = 80;

            var remainingWidth = availableWidth - numberWidth - coverWidth - durationWidth - playsWidth - actionsWidth;
            if (remainingWidth <= 300)
                return;

            gridView.Columns[0].Width = numberWidth;
            gridView.Columns[1].Width = coverWidth;
            gridView.Columns[2].Width = Math.Max(150, remainingWidth * 0.4);
            gridView.Columns[3].Width = Math.Max(120, remainingWidth * 0.3);
            gridView.Columns[4].Width = Math.Max(120, remainingWidth * 0.3);
            gridView.Columns[5].Width = durationWidth;
            gridView.Columns[6].Width = playsWidth;
            gridView.Columns[7].Width = actionsWidth;
        }

        private void PlaylistSongsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!MainWindow.IsItemActivation<ListViewItem>(e))
                return;

            if (PlaylistSongsListView.SelectedItem is Song song)
                _viewModel.PlaySongCommand.Execute(song);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
