using System;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>Splits a track into a vocal stem and an instrumental stem.</summary>
    public interface IAudioSeparatorService
    {
        event EventHandler<AudioProgressEventArgs>? ProgressChanged;

        bool IsAvailable { get; }

        /// <summary>Where finished stems are kept, outside the build output.</summary>
        string StemsRootDirectory { get; }

        Task<DependencyCheck> CheckDependenciesAsync();

        Task<bool> SeparateAudioAsync(string inputFilePath, CancellationToken cancellationToken = default);

        (string vocalsPath, string accompanimentPath) GetStemPaths(string inputFilePath);

        bool StemsExist(string inputFilePath);
    }
}
