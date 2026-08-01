using System.Collections.Generic;
using System.Collections.ObjectModel;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    public interface IPlaylistRepository
    {
        ObservableCollection<Playlist> Playlists { get; }

        Playlist Create(string name, string description = "", byte[]? coverImage = null);
        void Delete(Playlist playlist);
        void AddSong(Playlist playlist, Song song);
        void RemoveSong(Playlist playlist, Song song);
        void Save();

        /// <summary>
        /// Points playlist entries at the library's song instances so that edits and play counts
        /// are shared rather than duplicated.
        /// </summary>
        void RelinkTo(IEnumerable<Song> librarySongs);
    }
}
