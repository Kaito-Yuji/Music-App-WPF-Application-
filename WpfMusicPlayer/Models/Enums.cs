namespace WpfMusicPlayer.Models
{
    /// <summary>
    /// What happens when a track ends. Independent of shuffle, which only changes the order.
    /// </summary>
    public enum RepeatMode
    {
        Off,
        RepeatAll,
        RepeatOne
    }

    public enum PlaybackState
    {
        Stopped,
        Playing,
        Paused
    }
}
