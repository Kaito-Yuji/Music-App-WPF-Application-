using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WpfMusicPlayer.Models;
using WpfMusicPlayer.Services;

namespace WpfMusicPlayer.ViewModels
{
    public class PlaylistDetailsViewModel : ObservableObject
    {
        private readonly AppServices _services;
        private readonly IDialogService _dialogs;

        private string _searchText = string.Empty;
        private string _statusText = string.Empty;

        public PlaylistDetailsViewModel(Playlist playlist, AppServices services, IDialogService dialogs)
        {
            Playlist = playlist;
            _services = services;
            _dialogs = dialogs;

            PlayAllCommand = new RelayCommand(PlayAll);
            EditPlaylistCommand = new RelayCommand(EditPlaylist);
            AddSongsCommand = new RelayCommand(AddSongs);
            RemoveSongCommand = new RelayCommand<Song>(RemoveSong);
            PlaySongCommand = new RelayCommand<Song>(PlaySong);

            RefreshSongs();
        }

        public Playlist Playlist { get; }

        public ObservableCollection<Song> Songs { get; } = new();

        public RelayCommand PlayAllCommand { get; }
        public RelayCommand EditPlaylistCommand { get; }
        public RelayCommand AddSongsCommand { get; }
        public RelayCommand<Song> RemoveSongCommand { get; }
        public RelayCommand<Song> PlaySongCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    RefreshSongs();
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private void RefreshSongs()
        {
            Songs.Clear();

            // Always filter the playlist itself, never the previous result, so clearing the box
            // brings every song back
            var source = Playlist.Songs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var query = SearchText.ToLowerInvariant();
                source = source.Where(s =>
                    s.Title.ToLowerInvariant().Contains(query) ||
                    s.Artist.ToLowerInvariant().Contains(query) ||
                    s.Album.ToLowerInvariant().Contains(query));
            }

            foreach (var song in source)
                Songs.Add(song);

            StatusText = $"{Songs.Count} songs displayed";
        }

        private void PlayAll()
        {
            if (Songs.Count == 0)
                return;

            _services.Playback.SetQueue(Songs, 0);
            _services.Playback.PlaySongAtIndex(0);
            StatusText = "Playing playlist...";
        }

        private void PlaySong(Song? song)
        {
            if (song == null)
                return;

            _services.Playback.PlaySongFromCollection(song, Songs);
            StatusText = $"Now playing: {song.Title}";
        }

        private void EditPlaylist()
        {
            var details = _dialogs.AskForPlaylistDetails(
                new PlaylistDetails(Playlist.Name, Playlist.Description, Playlist.CoverImage));

            if (details == null || string.IsNullOrWhiteSpace(details.Name))
                return;

            Playlist.Name = details.Name;
            Playlist.Description = details.Description;

            if (details.Cover != null)
                Playlist.CoverImage = details.Cover;

            _services.Playlists.Save();
            StatusText = "Playlist updated";
        }

        private void AddSongs()
        {
            var selected = _dialogs.PickSongs(_services.Library.Songs, Playlist.Songs);
            if (selected.Count == 0)
                return;

            foreach (var song in selected)
                _services.Playlists.AddSong(Playlist, song);

            RefreshSongs();
            StatusText = $"Added {selected.Count} songs to the playlist";
        }

        private void RemoveSong(Song? song)
        {
            if (song == null)
                return;

            if (!_dialogs.Confirm($"Remove '{song.Title}' from this playlist?", "Confirm Remove"))
                return;

            _services.Playlists.RemoveSong(Playlist, song);
            RefreshSongs();
            StatusText = "Song removed from the playlist";
        }
    }
}
