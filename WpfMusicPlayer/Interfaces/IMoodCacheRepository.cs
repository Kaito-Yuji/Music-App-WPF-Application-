using System.Collections.Generic;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Interfaces
{
    /// <summary>
    /// Storage for mood results, keyed by lower-cased file path. Analysis is expensive, so a song
    /// is only ever measured once.
    /// </summary>
    public interface IMoodCacheRepository
    {
        Dictionary<string, MoodAnalysis> LoadAll();

        void SaveAll(Dictionary<string, MoodAnalysis> moods);
    }
}
