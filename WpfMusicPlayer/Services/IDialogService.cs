using System.Collections.Generic;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public enum DialogIcon
    {
        Information,
        Warning,
        Error
    }

    public record PlaylistDetails(string Name, string Description, byte[]? Cover);

    /// <summary>Outcome of editing a song's tags. Raw artwork is kept separate from the
    /// display thumbnail so the original image is what gets written back to the file.</summary>
    public record SongEditResult(Song Edited, bool AlbumArtChanged, byte[]? RawAlbumArt);

    /// <summary>
    /// Everything the view models need from the window system. Keeping it behind an interface is
    /// what stops view models from depending on WPF window types directly.
    /// </summary>
    public interface IDialogService
    {
        string? PickFolder(string description);
        string? PickSaveFile(string title, string filter, string defaultFileName);

        PlaylistDetails? AskForPlaylistDetails(PlaylistDetails? initial = null);
        Playlist? PickPlaylist(IEnumerable<Playlist> playlists);
        IReadOnlyList<Song> PickSongs(IEnumerable<Song> allSongs, IEnumerable<Song> alreadyIncluded);
        SongEditResult? EditSong(Song song);

        void ShowPlaylistDetails(Playlist playlist);
        void ShowStatistics();

        void ShowMessage(string message, string title, DialogIcon icon = DialogIcon.Information);
        bool Confirm(string message, string title);
    }
}
