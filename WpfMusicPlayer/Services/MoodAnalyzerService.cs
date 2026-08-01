using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Interfaces;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Estimates the emotional character of a track by running the librosa-based Python script and
    /// caching what it reports. Results are keyed by file path and survive restarts, so a track is
    /// only ever analysed once.
    /// </summary>
    public class MoodAnalyzerService : IMoodAnalyzerService
    {
        private const string ResultPrefix = "MOOD_RESULT ";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly PythonRunner _python;
        private readonly IMoodCacheRepository _cacheRepository;
        private readonly string _scriptPath;
        private readonly Dictionary<string, MoodAnalysis> _cache;

        public MoodAnalyzerService(PythonRunner python, IMoodCacheRepository cacheRepository)
        {
            _python = python;
            _cacheRepository = cacheRepository;
            _scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "mood_detector.py");

            _cache = _cacheRepository.LoadAll();
        }

        public event EventHandler<string>? MoodAnalyzed;

        public bool IsAvailable => _python.IsAvailable && File.Exists(_scriptPath);

        public async Task<DependencyCheck> CheckDependenciesAsync()
        {
            if (!File.Exists(_scriptPath) && _python.IsAvailable)
            {
                return new DependencyCheck(false,
                    $"The mood detection script is missing:\n{_scriptPath}\n\nRebuild the project to restore it.");
            }

            // librosa pulls in numpy itself, but naming both makes the error message specific
            return await _python.CheckModulesAsync(new[] { "numpy", "librosa" }, requireFfmpeg: true);
        }

        public MoodAnalysis? GetMood(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            return _cache.TryGetValue(Normalise(filePath), out var analysis) ? analysis : null;
        }

        public async Task<MoodAnalysis?> AnalyzeAsync(Song song, CancellationToken cancellationToken = default)
        {
            var cached = GetMood(song.FilePath);
            if (cached != null)
                return cached;

            if (!IsAvailable || !File.Exists(song.FilePath))
                return null;

            var result = await _python.RunAsync($"\"{_scriptPath}\" \"{song.FilePath}\"", cancellationToken);

            if (result.ExitCode != 0)
            {
                Debug.WriteLine($"Mood analysis failed for '{song.Title}': {result.StandardError}");
                return null;
            }

            var analysis = ParseResult(result.StandardOutput);
            if (analysis == null)
            {
                Debug.WriteLine($"Mood analysis produced no result for '{song.Title}'");
                return null;
            }

            _cache[Normalise(song.FilePath)] = analysis;
            Save();

            song.Mood = analysis.Mood;
            song.MoodDetails = analysis.Describe();

            MoodAnalyzed?.Invoke(this, song.FilePath);

            return analysis;
        }

        public async Task AnalyzeManyAsync(IEnumerable<Song> songs, IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var pending = songs.Where(s => GetMood(s.FilePath) == null).ToList();

            for (var i = 0; i < pending.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report($"Analysing {i + 1}/{pending.Count}: {pending[i].Title}");
                await AnalyzeAsync(pending[i], cancellationToken);
            }

            progress?.Report(pending.Count == 0
                ? "Every song listed has already been analysed"
                : $"Finished analysing {pending.Count} songs");
        }

        /// <summary>
        /// Picks the payload out of stdout. librosa and its dependencies write plenty of other
        /// lines, which is why the script marks its result with a prefix.
        /// </summary>
        private static MoodAnalysis? ParseResult(string standardOutput)
        {
            var line = standardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .LastOrDefault(l => l.StartsWith(ResultPrefix, StringComparison.Ordinal));

            if (line == null)
                return null;

            try
            {
                var payload = JsonSerializer.Deserialize<MoodPayload>(line[ResultPrefix.Length..], SerializerOptions);
                if (payload == null)
                    return null;

                return new MoodAnalysis
                {
                    Mood = Enum.TryParse<Mood>(payload.Mood, ignoreCase: true, out var mood) ? mood : Mood.Unknown,
                    Valence = payload.Valence,
                    Arousal = payload.Arousal,
                    Tempo = payload.Tempo,
                    Key = payload.Key ?? string.Empty,
                    Scale = payload.Scale ?? string.Empty
                };
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Could not read the mood result: {ex.Message}");
                return null;
            }
        }

        private static string Normalise(string filePath) => filePath.ToLowerInvariant();

        private void Save() => _cacheRepository.SaveAll(_cache);

        private class MoodPayload
        {
            public string? Mood { get; set; }
            public double Valence { get; set; }
            public double Arousal { get; set; }
            public double Tempo { get; set; }
            public string? Key { get; set; }
            public string? Scale { get; set; }
        }
    }
}
