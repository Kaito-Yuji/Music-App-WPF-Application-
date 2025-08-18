namespace WpfMusicPlayer.Models
{
    public enum PlayMode
    {
        Normal,
        RepeatOne,
        RepeatAll,
        Shuffle
    }

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
