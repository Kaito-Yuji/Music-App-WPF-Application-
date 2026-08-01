using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>Ties stem separation to playback: play the instrumental, then switch back.</summary>
    public interface IKaraokeService : INotifyPropertyChanged
    {
        bool IsAvailable { get; }
        bool IsInKaraokeMode { get; }

        event EventHandler<AudioProgressEventArgs>? Progress;

        bool StemsExist(Song song);

        Task<DependencyCheck> CheckDependenciesAsync();

        Task<bool> EnableAsync(CancellationToken cancellationToken = default);

        bool Disable();

        Task<bool> ExportAsync(Song song, string outputPath, bool instrumental,
            CancellationToken cancellationToken = default);

        Task PreprocessAsync(IEnumerable<Song> songs, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
