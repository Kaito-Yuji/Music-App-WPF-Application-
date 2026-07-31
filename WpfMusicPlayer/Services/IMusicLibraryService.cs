using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    public interface IMusicLibraryService
    {
        ObservableCollection<Song> Songs { get; }

        Task ScanFolderAsync(string folderPath, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);

        Task AddFilesAsync(IEnumerable<string> filePaths);

        IReadOnlyList<Song> Search(string query);

        /// <summary>
        /// Writes the song's tags back to its file. Artwork in the file is left untouched unless
        /// <paramref name="albumArtChanged"/> is set, so editing a title cannot silently replace
        /// full-size cover art with the display thumbnail.
        /// </summary>
        void UpdateMetadata(Song song, bool albumArtChanged = false, byte[]? replacementAlbumArt = null);
    }
}
