using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WpfMusicPlayer.Services
{
    public class AudioSeparatorService
    {
        // Event for progress reporting
        public event EventHandler<ProgressEventArgs>? ProgressChanged;

        public class ProgressEventArgs : EventArgs
        {
            public string Message { get; set; } = string.Empty;
            public int? Percentage { get; set; }
            public bool IsCompleted { get; set; }
            public bool IsError { get; set; }
        }

        // The wrapper script reports its progress as lines like "Progress: 45%"
        private static readonly Regex ProgressRegex = new(@"^Progress:\s*(\d+)\s*%", RegexOptions.Compiled);

        private readonly PythonRunner _python;
        private readonly string _scriptPath;
        private readonly string _stemsRootDirectory;

        public AudioSeparatorService(PythonRunner python)
        {
            _python = python;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _scriptPath = Path.Combine(baseDir, "Resources", "audio_separator_wrapper.py");

            // Stems live beside the other user data rather than under bin/, so a Clean or
            // Rebuild does not throw away results that took many minutes to produce.
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _stemsRootDirectory = Path.Combine(appDataPath, "WpfMusicPlayer", "stems");
        }

        public bool IsAvailable => _python.IsAvailable && File.Exists(_scriptPath);

        public string StemsRootDirectory => _stemsRootDirectory;

        public async Task<DependencyCheck> CheckDependenciesAsync()
        {
            if (!File.Exists(_scriptPath) && _python.IsAvailable)
            {
                return new DependencyCheck(false,
                    $"The separation script is missing:\n{_scriptPath}\n\nRebuild the project to restore it.");
            }

            return await _python.CheckModulesAsync(
                new[] { "audio_separator", "onnxruntime" }, requireFfmpeg: true);
        }

        public async Task<bool> SeparateAudioAsync(string inputFilePath, CancellationToken cancellationToken = default)
        {
            if (!IsAvailable || !File.Exists(inputFilePath))
                return false;

            var stemDirectoryName = GetStemDirectoryName(inputFilePath);
            Directory.CreateDirectory(_stemsRootDirectory);

            var arguments =
                $"\"{_scriptPath}\" \"{inputFilePath}\" \"{_stemsRootDirectory}\" --name \"{stemDirectoryName}\"";

            ReportProgress("Starting audio separation...", 5);

            var lastMessage = "Separating vocals and instruments...";

            try
            {
                var result = await _python.RunAsync(arguments, cancellationToken, line =>
                {
                    var match = ProgressRegex.Match(line);

                    if (match.Success && int.TryParse(match.Groups[1].Value, out var percentage))
                    {
                        ReportProgress(lastMessage, percentage);
                    }
                    else
                    {
                        lastMessage = line;
                        ReportProgress(line);
                    }
                });

                if (result.ExitCode == 0 && StemsExist(inputFilePath))
                {
                    ReportProgress("Audio separation completed successfully!", 100, true);
                    return true;
                }

                ReportProgress(BuildFailureMessage(result.StandardError), 0, true, true);
                return false;
            }
            catch (OperationCanceledException)
            {
                ReportProgress("Audio separation cancelled.", 0, true);
                return false;
            }
        }

        private static string BuildFailureMessage(string standardError)
        {
            // Keep only the tail: the traceback header is rarely the useful part
            var details = standardError
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .TakeLast(5)
                .ToList();

            return details.Count == 0
                ? "Audio separation failed"
                : $"Audio separation failed:{Environment.NewLine}{string.Join(Environment.NewLine, details)}";
        }

        private void ReportProgress(string message, int? percentage = null, bool isCompleted = false, bool isError = false)
        {
            ProgressChanged?.Invoke(this, new ProgressEventArgs
            {
                Message = message,
                Percentage = percentage,
                IsCompleted = isCompleted,
                IsError = isError
            });
        }

        public (string vocalsPath, string accompanimentPath) GetStemPaths(string inputFilePath)
        {
            var stemDirectory = Path.Combine(_stemsRootDirectory, GetStemDirectoryName(inputFilePath));

            return (Path.Combine(stemDirectory, "vocals.wav"),
                    Path.Combine(stemDirectory, "accompaniment.wav"));
        }

        public bool StemsExist(string inputFilePath)
        {
            var (vocalsPath, accompanimentPath) = GetStemPaths(inputFilePath);
            return File.Exists(vocalsPath) && File.Exists(accompanimentPath);
        }

        /// <summary>
        /// Folder name for a song's stems. The readable part is truncated, so a short hash of the
        /// full path is appended - otherwise two songs sharing their first 30 characters would
        /// overwrite each other's stems.
        /// </summary>
        private static string GetStemDirectoryName(string inputFilePath)
        {
            var readable = SanitizeFilename(Path.GetFileNameWithoutExtension(inputFilePath));

            var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(inputFilePath.ToLowerInvariant()));
            var shortHash = Convert.ToHexString(hashBytes)[..8].ToLowerInvariant();

            return $"{readable}_{shortHash}";
        }

        private static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "unknown";

            var sanitized = filename;

            sanitized = Regex.Replace(sanitized, @"[<>:""/\\|?*]", "_");
            sanitized = Regex.Replace(sanitized, @"[()[\]{}]", "_");
            sanitized = Regex.Replace(sanitized, @"[&%$#@!]", "_");
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            sanitized = sanitized.Trim('_');

            if (sanitized.Length > 30)
                sanitized = sanitized.Substring(0, 30).TrimEnd('_');

            return string.IsNullOrEmpty(sanitized) ? "audio_file" : sanitized;
        }
    }
}
