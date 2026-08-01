using System;

namespace WpfMusicPlayer.Models
{
    /// <summary>
    /// Progress of a long-running audio job, reported while a Python script works.
    /// </summary>
    public class AudioProgressEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Percentage complete, when the script reports one.</summary>
        public int? Percentage { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsError { get; set; }
    }
}
