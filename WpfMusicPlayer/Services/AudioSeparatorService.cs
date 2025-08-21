using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

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
        private readonly string _pythonExecutablePath;
        private readonly string _audioSeparatorScriptPath;
        private readonly bool _isAvailable;

        public AudioSeparatorService()
        {
            _pythonExecutablePath = GetPythonPath();
            
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _audioSeparatorScriptPath = Path.Combine(baseDir, "Resources", "audio_separator_wrapper.py");
            
            _isAvailable = !string.IsNullOrEmpty(_pythonExecutablePath) && File.Exists(_audioSeparatorScriptPath);
        }

        private string GetPythonPath()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(5000);
                
                if (process.ExitCode == 0)
                {
                    return "python";
                }
            }
            catch { }

            return "";
        }

        public bool IsAvailable => _isAvailable;

        public async Task<bool> SeparateAudioAsync(string inputFilePath)
        {
            if (!_isAvailable || !File.Exists(inputFilePath))
            {
                return false;
            }

            var resourcesDir = Path.GetDirectoryName(_audioSeparatorScriptPath);
            var outputDirectory = Path.Combine(resourcesDir!, "test_fixed");
            Directory.CreateDirectory(outputDirectory);

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _pythonExecutablePath,
                Arguments = $"\"{_audioSeparatorScriptPath}\" \"{inputFilePath}\" \"{outputDirectory}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ReportProgress("Starting audio separation...", 5);

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    var (vocalsPath, accompanimentPath) = GetSeparatedFilePathsTestFixed(inputFilePath);
                    
                    if (File.Exists(vocalsPath) && File.Exists(accompanimentPath))
                    {
                        ReportProgress("Audio separation completed successfully!", 100, true);
                        return true;
                    }
                }

                ReportProgress("Audio separation failed", 0, true, true);
                return false;
            }
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

        public (string vocalsPath, string accompanimentPath) GetSeparatedFilePaths(string inputFilePath, string outputDirectory)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            var stemDirectory = Path.Combine(outputDirectory, fileNameWithoutExtension);
            
            var vocalsPath = Path.Combine(stemDirectory, "vocals.wav");
            var accompanimentPath = Path.Combine(stemDirectory, "accompaniment.wav");
            
            return (vocalsPath, accompanimentPath);
        }

        public bool StemsExist(string inputFilePath, string outputDirectory)
        {
            var (vocalsPath, accompanimentPath) = GetSeparatedFilePaths(inputFilePath, outputDirectory);
            return File.Exists(vocalsPath) && File.Exists(accompanimentPath);
        }

        public (string vocalsPath, string accompanimentPath) GetSeparatedFilePathsTestFixed(string inputFilePath)
        {
            var resourcesDir = Path.GetDirectoryName(_audioSeparatorScriptPath);
            var testFixedDir = Path.Combine(resourcesDir!, "test_fixed");
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            
            var sanitizedFileName = SanitizePythonFilename(fileNameWithoutExtension);
            var stemDirectory = Path.Combine(testFixedDir, sanitizedFileName);
            
            var vocalsPath = Path.Combine(stemDirectory, "vocals.wav");
            var accompanimentPath = Path.Combine(stemDirectory, "accompaniment.wav");
            
            return (vocalsPath, accompanimentPath);
        }

        private string SanitizePythonFilename(string filename)
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

        public bool StemsExistTestFixed(string inputFilePath)
        {
            var (vocalsPath, accompanimentPath) = GetSeparatedFilePathsTestFixed(inputFilePath);
            return File.Exists(vocalsPath) && File.Exists(accompanimentPath);
        }
    }
}
