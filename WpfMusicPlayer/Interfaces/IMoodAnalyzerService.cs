using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>Estimates the emotional character of a track from its audio.</summary>
    public interface IMoodAnalyzerService
    {
        bool IsAvailable { get; }

        /// <summary>Raised as each track finishes, carrying its file path.</summary>
        event EventHandler<string>? MoodAnalyzed;

        Task<DependencyCheck> CheckDependenciesAsync();

        /// <summary>Cached result for a file, or null when it has never been analysed.</summary>
        MoodAnalysis? GetMood(string filePath);

        Task<MoodAnalysis?> AnalyzeAsync(Song song, CancellationToken cancellationToken = default);

        Task AnalyzeManyAsync(IEnumerable<Song> songs, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default);
    }
}
