using System.Collections.Generic;

namespace WpfMusicPlayer.Models
{
    /// <summary>
    /// What the listener was doing when the app last closed: which library, how loud, which queue
    /// and where in the current track.
    /// </summary>
    public class AppState
    {
        public string MusicFolderPath { get; set; } = string.Empty;
        public float Volume { get; set; } = 0.8f;
        public bool IsShuffleOn { get; set; }
        public RepeatMode RepeatMode { get; set; } = RepeatMode.Off;

        public List<string> QueueFilePaths { get; set; } = new();
        public int CurrentIndex { get; set; } = -1;
        public double PositionSeconds { get; set; }
    }
}
