namespace WpfMusicPlayer.Models
{
    /// <summary>
    /// Where a track sits on the classic two-axis model of emotion: how positive it feels
    /// (valence) against how activating it feels (arousal). Each value is one quadrant.
    /// </summary>
    public enum Mood
    {
        /// <summary>Not analysed yet.</summary>
        Unknown,

        /// <summary>Positive and activating - bright, upbeat, major key.</summary>
        Happy,

        /// <summary>Activating but dark - driving, intense, heavy.</summary>
        Energetic,

        /// <summary>Positive but relaxed - warm, gentle, easy listening.</summary>
        Calm,

        /// <summary>Dark and subdued - slow, quiet, minor key.</summary>
        Sad
    }

    /// <summary>
    /// A mood estimate together with the measurements it was derived from, so the verdict can be
    /// explained rather than just asserted.
    /// </summary>
    public class MoodAnalysis
    {
        public Mood Mood { get; set; } = Mood.Unknown;

        /// <summary>How positive the track sounds, 0 to 1.</summary>
        public double Valence { get; set; }

        /// <summary>How activating the track sounds, 0 to 1.</summary>
        public double Arousal { get; set; }

        public double Tempo { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Scale { get; set; } = string.Empty;

        public string Describe()
        {
            if (Mood == Mood.Unknown)
                return "Not analysed yet";

            var scale = string.IsNullOrEmpty(Scale) ? "" : $"{Key} {Scale}, ";

            return $"{MoodLabel(Mood)}\n" +
                   $"{scale}{Tempo:0} BPM\n" +
                   $"Positivity {Valence:P0}, energy {Arousal:P0}";
        }

        public static string MoodLabel(Mood mood) => mood switch
        {
            Mood.Happy => "Happy - bright and upbeat",
            Mood.Energetic => "Energetic - intense and driving",
            Mood.Calm => "Calm - relaxed and warm",
            Mood.Sad => "Sad - slow and subdued",
            _ => "Unknown"
        };
    }
}
