namespace WpfMusicPlayer.Models
{
    /// <summary>
    /// Result of probing the Python side for everything a feature needs. Lives here rather than
    /// beside the runner so the service interfaces can name it without depending on any
    /// implementation.
    /// </summary>
    /// <param name="IsSatisfied">True when every dependency was found.</param>
    /// <param name="Message">Plain-language explanation, including how to fix it when not.</param>
    public record DependencyCheck(bool IsSatisfied, string Message);

    /// <summary>Raw output of a finished Python process.</summary>
    public record PythonResult(int ExitCode, string StandardOutput, string StandardError);
}
