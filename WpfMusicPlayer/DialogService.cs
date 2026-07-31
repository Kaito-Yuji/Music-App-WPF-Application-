using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;
using WpfMusicPlayer.ViewModels;

namespace WpfMusicPlayer
{
    /// <summary>
    /// The only place that knows about windows and message boxes. View models depend on
    /// <see cref="IDialogService"/> instead, which keeps them free of WPF.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly AppServices _services;

        public DialogService(AppServices services)
        {
            _services = services;
        }

        private static Window? Owner => Application.Current?.MainWindow;

        public string? PickFolder(string description)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog { Description = description };

            var accepted = Owner != null ? dialog.ShowDialog(Owner) : dialog.ShowDialog();
            return accepted == true ? dialog.SelectedPath : null;
        }

        public string? PickSaveFile(string title, string filter, string defaultFileName)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FileName = SanitizeFileName(defaultFileName),
                DefaultExt = "wav"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public PlaylistDetails? AskForPlaylistDetails(PlaylistDetails? initial = null)
        {
            var dialog = new PlaylistNameDialog
            {
                Owner = Owner,
                PlaylistName = initial?.Name ?? string.Empty,
                PlaylistDescription = initial?.Description ?? string.Empty
            };

            if (dialog.ShowDialog() != true)
                return null;

            return new PlaylistDetails(dialog.PlaylistName, dialog.PlaylistDescription, dialog.PlaylistCover);
        }

        public Playlist? PickPlaylist(IEnumerable<Playlist> playlists)
        {
            var dialog = new PlaylistSelectionDialog(playlists.ToList()) { Owner = Owner };
            return dialog.ShowDialog() == true ? dialog.SelectedPlaylist : null;
        }

        public IReadOnlyList<Song> PickSongs(IEnumerable<Song> allSongs, IEnumerable<Song> alreadyIncluded)
        {
            var dialog = new SongSelectionDialog(allSongs, alreadyIncluded) { Owner = Owner };
            return dialog.ShowDialog() == true ? dialog.SelectedSongs : new List<Song>();
        }

        public SongEditResult? EditSong(Song song)
        {
            var dialog = new SongEditDialog(song) { Owner = Owner };

            if (dialog.ShowDialog() != true || !dialog.WasModified)
                return null;

            return new SongEditResult(dialog.EditedSong, dialog.AlbumArtChanged, dialog.RawAlbumArt);
        }

        public void ShowPlaylistDetails(Playlist playlist)
        {
            var window = new PlaylistDetailsWindow(new PlaylistDetailsViewModel(playlist, _services, this))
            {
                Owner = Owner
            };

            window.ShowDialog();
        }

        public void ShowStatistics()
        {
            var window = new ListeningStatsWindow(
                new ListeningStatsViewModel(_services.Stats, _services.Library))
            {
                Owner = Owner
            };

            window.ShowDialog();
        }

        public void ShowMessage(string message, string title, DialogIcon icon = DialogIcon.Information)
        {
            // The owner is null while the main window is still being created
            var owner = Owner;

            if (owner == null)
                MessageBox.Show(message, title, MessageBoxButton.OK, ToImage(icon));
            else
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, ToImage(icon));
        }

        public bool Confirm(string message, string title)
        {
            var owner = Owner;

            var result = owner == null
                ? MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                : MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        private static MessageBoxImage ToImage(DialogIcon icon) => icon switch
        {
            DialogIcon.Warning => MessageBoxImage.Warning,
            DialogIcon.Error => MessageBoxImage.Error,
            _ => MessageBoxImage.Information
        };

        private static string SanitizeFileName(string fileName)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return new string(fileName.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        }
    }
}
