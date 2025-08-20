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
            // Get the full path to Python executable
            _pythonExecutablePath = GetPythonPath();
            
            // Get the path to the audio separator script
            // The script should be in the project's Resources folder
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // For debug builds, we need to go up from bin/Debug/net8.0-windows/ to the project root
            var projectRoot = Directory.GetParent(baseDir)?.Parent?.Parent?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                _audioSeparatorScriptPath = Path.Combine(projectRoot, "Resources", "audio_separator_wrapper.py");
            }
            else
            {
                // Fallback: try the original location
                _audioSeparatorScriptPath = Path.Combine(baseDir, "Resources", "audio_separator_wrapper.py");
            }
            
            // Check if both Python and the script are available
            _isAvailable = !string.IsNullOrEmpty(_pythonExecutablePath) && File.Exists(_audioSeparatorScriptPath);
            
            if (_isAvailable)
            {
                // Test if audio-separator is properly installed
                _isAvailable = TestAudioSeparatorInstallation();
            }
        }

        /// <summary>
        /// Gets the full path to the Python executable
        /// </summary>
        /// <returns>Full path to python.exe or empty string if not found</returns>
        private string GetPythonPath()
        {
            try
            {
                // First, try to use 'python' directly from PATH
                try
                {
                    var pathTestProcess = new Process
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
                    pathTestProcess.Start();
                    pathTestProcess.WaitForExit(5000);
                    
                    if (pathTestProcess.ExitCode == 0)
                    {
                        // Python is available in PATH, just return "python"
                        return "python";
                    }
                }
                catch
                {
                    // Continue to try specific paths
                }

                // Try common Python installation locations
                string[] possiblePaths = {
                    @"C:\Python313\python.exe",
                    @"C:\Python312\python.exe", 
                    @"C:\Python311\python.exe",
                    @"C:\Python310\python.exe",
                    @"C:\Python39\python.exe",
                    @"C:\Python38\python.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python313", "python.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python312", "python.exe"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311", "python.exe")
                };

                // Check each possible path
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                // If none found, try to search PATH environment variable more thoroughly
                var pathVar = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathVar))
                {
                    var paths = pathVar.Split(';');
                    foreach (var path in paths)
                    {
                        var pythonExe = Path.Combine(path.Trim(), "python.exe");
                        if (File.Exists(pythonExe))
                        {
                            return pythonExe;
                        }
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Tests if audio-separator is properly installed and can be used
        /// </summary>
        /// <returns>True if audio-separator is available and working</returns>
        private bool TestAudioSeparatorInstallation()
        {
            try
            {
                var testProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pythonExecutablePath,
                        Arguments = $"\"{_audioSeparatorScriptPath}\" --help",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                testProcess.Start();
                testProcess.WaitForExit(10000); // 10 second timeout
                
                var output = testProcess.StandardOutput.ReadToEnd();
                var error = testProcess.StandardError.ReadToEnd();
                
                // Help command should return exit code 1 (expected for argparse)
                // and output should contain help information about separating audio
                return testProcess.ExitCode == 1 && 
                       (output.Contains("Separate audio") || output.Contains("vocals and accompaniment"));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the audio separator service is available and ready to use
        /// </summary>
        public bool IsAvailable => _isAvailable;

        /// <summary>
        /// Separates a song into stems (vocals and accompaniment) using audio-separator
        /// </summary>
        /// <param name="inputFilePath">Path to the input audio file (can be anywhere on the system)</param>
        /// <param name="outputDirectory">Directory where separated stems will be saved</param>
        /// <returns>True if separation was successful, false otherwise</returns>
        public async Task<bool> SeparateAudioAsync(string inputFilePath, string outputDirectory)
        {
            try
            {
                // if (!_isAvailable)
                // {
                //     ReportProgress("Audio Separator is not available. Please ensure Python and audio-separator are properly installed.", 0, true, true);
                //     return false;
                // }

                // Validate input file exists
                if (!File.Exists(inputFilePath))
                {
                    ReportProgress($"Input file not found: {inputFilePath}", 0, true, true);
                    return false;
                }

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputDirectory);

                // Get the file name for the output folder naming - sanitize for safe path creation
                var inputFileName = Path.GetFileName(inputFilePath);
                var sanitizedFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(inputFileName));
                
                // Use a shorter, unique output subdirectory name to avoid path length issues
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var outputSubDir = $"temp_{sanitizedFileName}_{timestamp}";
                
                // Ensure the subdirectory name isn't too long (Windows path limit is 260 characters)
                if (outputSubDir.Length > 50)
                {
                    outputSubDir = $"temp_{timestamp}_{Path.GetRandomFileName().Replace(".", "")}";
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutablePath,
                    Arguments = $"\"{_audioSeparatorScriptPath}\" \"{inputFilePath}\" \"{outputSubDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_audioSeparatorScriptPath) // Set working directory to Resources folder
                };

                // Set environment variables to suppress warnings
                processStartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1"; // Force unbuffered output
                processStartInfo.EnvironmentVariables["PYTHONWARNINGS"] = "ignore"; // Suppress Python warnings

                ReportProgress("Starting audio separation...", 5);

                // Debug logging
                ReportProgress($"Using output subdirectory: {outputSubDir}", null);
                ReportProgress($"Working directory: {Path.GetDirectoryName(_audioSeparatorScriptPath)}", null);
                ReportProgress("Audio separation may take 5-30 minutes depending on song length...", null);

                // Start the process
                using (var process = new Process())
                {
                    process.StartInfo = processStartInfo;
                    process.Start();

                    // Read output in real-time for progress reporting
                    var outputLines = new List<string>();
                    var errorLines = new List<string>();

                    // Create tasks to read output and error streams
                    var outputTask = ReadOutputAsync(process.StandardOutput, outputLines);
                    var errorTask = ReadOutputAsync(process.StandardError, errorLines);

                    // Wait for the process to complete with timeout
                    bool finished = process.WaitForExit(1800000); // 30 minute timeout
                    
                    if (!finished)
                    {
                        process.Kill();
                        ReportProgress("Audio separation process timed out after 30 minutes", 0, true, true);
                        return false;
                    }

                    await Task.WhenAll(outputTask, errorTask);

                    var output = string.Join("\n", outputLines);
                    var error = string.Join("\n", errorLines);

                    // Check if the process completed successfully
                    if (process.ExitCode == 0)
                    {
                        ReportProgress("Audio separation completed successfully", 90);
                        
                        // The Python script creates files in Resources/separated_{filename}_{timestamp}/{filename}/
                        // We need to move them to the expected output directory structure
                        var resourcesDir = Path.GetDirectoryName(_audioSeparatorScriptPath);
                        if (string.IsNullOrEmpty(resourcesDir))
                        {
                            ReportProgress("Unable to determine Resources directory path", 0, true, true);
                            return false;
                        }
                        
                        var sourceDir = Path.Combine(resourcesDir, outputSubDir, sanitizedFileName);
                        
                        if (Directory.Exists(sourceDir))
                        {
                            ReportProgress("Moving separated files to final location...", 95);
                            
                            // Create the target directory structure that matches our expected paths
                            var targetDir = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFilePath));
                            Directory.CreateDirectory(targetDir);
                            
                            // Move the separated files to the expected location
                            var sourceFiles = Directory.GetFiles(sourceDir, "*.wav");
                            foreach (var sourceFile in sourceFiles)
                            {
                                var fileName = Path.GetFileName(sourceFile);
                                var targetFile = Path.Combine(targetDir, fileName);
                                
                                // Move the file, overwriting if it exists
                                if (File.Exists(targetFile))
                                {
                                    File.Delete(targetFile);
                                }
                                File.Move(sourceFile, targetFile);
                            }
                            
                            // Clean up the source directory
                            try
                            {
                                Directory.Delete(sourceDir, true);
                                
                                // Also try to clean up the parent separated directory if it's empty
                                var separatedDir = Path.Combine(resourcesDir, outputSubDir);
                                if (Directory.Exists(separatedDir) && !Directory.EnumerateFileSystemEntries(separatedDir).Any())
                                {
                                    Directory.Delete(separatedDir);
                                }
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }
                        }

                        // Verify that the expected output files were created in the target location
                        var (vocalsPath, accompanimentPath) = GetSeparatedFilePaths(inputFilePath, outputDirectory);
                        if (File.Exists(vocalsPath) && File.Exists(accompanimentPath))
                        {
                            ReportProgress("Audio separation completed successfully!", 100, true);
                            return true;
                        }
                        else
                        {
                            ReportProgress("Audio separation completed but expected output files were not found in the target directory", 0, true, true);
                            return false;
                        }
                    }
                    else
                    {
                        // Filter out common warnings that don't indicate failure
                        var filteredError = FilterWarnings(error);
                        ReportProgress($"Audio separation process failed with exit code {process.ExitCode}:\n{filteredError}", 0, true, true);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ReportProgress($"Error separating audio: {ex.Message}", 0, true, true);
                return false;
            }
        }

        /// <summary>
        /// Reads output from a stream and reports progress based on content
        /// </summary>
        private async Task ReadOutputAsync(StreamReader reader, List<string> lines)
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
                
                // Parse progress information from the output
                ParseProgressFromOutput(line);
            }
        }

        /// <summary>
        /// Parses progress information from Python script output
        /// </summary>
        private void ParseProgressFromOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return;

            // Look for percentage patterns like "34%" or "[34/100]" or "Progress: 34%"
            var percentageMatch = Regex.Match(output, @"(\d+)%");
            if (percentageMatch.Success)
            {
                if (int.TryParse(percentageMatch.Groups[1].Value, out int percentage))
                {
                    ReportProgress($"Processing... {percentage}%", percentage);
                    return;
                }
            }

            // Look for fraction patterns like "[34/100]" or "34/100"
            var fractionMatch = Regex.Match(output, @"\[?(\d+)/(\d+)\]?");
            if (fractionMatch.Success)
            {
                if (int.TryParse(fractionMatch.Groups[1].Value, out int current) && 
                    int.TryParse(fractionMatch.Groups[2].Value, out int total) && total > 0)
                {
                    int percentage = (current * 100) / total;
                    ReportProgress($"Processing... {current}/{total} ({percentage}%)", percentage);
                    return;
                }
            }

            // Look for specific status messages
            if (output.Contains("Loading model"))
            {
                ReportProgress("Loading AI model...", 10);
            }
            else if (output.Contains("Starting separation"))
            {
                ReportProgress("Starting separation process...", 20);
            }
            else if (output.Contains("Separating"))
            {
                ReportProgress("Separating audio tracks...", null);
            }
            else if (output.Contains("Saving"))
            {
                ReportProgress("Saving separated tracks...", 80);
            }
            else if (output.Contains("completed"))
            {
                ReportProgress("Separation completed!", 90);
            }
        }

        /// <summary>
        /// Reports progress to subscribers
        /// </summary>
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

        /// <summary>
        /// Gets the expected paths for separated stems
        /// </summary>
        /// <param name="inputFilePath">Original audio file path</param>
        /// <param name="outputDirectory">Output directory used for separation</param>
        /// <returns>Tuple containing paths to vocals and accompaniment files</returns>
        public (string vocalsPath, string accompanimentPath) GetSeparatedFilePaths(string inputFilePath, string outputDirectory)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            var stemDirectory = Path.Combine(outputDirectory, fileNameWithoutExtension);
            
            var vocalsPath = Path.Combine(stemDirectory, "vocals.wav");
            var accompanimentPath = Path.Combine(stemDirectory, "accompaniment.wav");
            
            return (vocalsPath, accompanimentPath);
        }

        /// <summary>
        /// Checks if separated stems already exist for a given audio file
        /// </summary>
        /// <param name="inputFilePath">Original audio file path</param>
        /// <param name="outputDirectory">Output directory to check</param>
        /// <returns>True if both stems exist, false otherwise</returns>
        public bool StemsExist(string inputFilePath, string outputDirectory)
        {
            var (vocalsPath, accompanimentPath) = GetSeparatedFilePaths(inputFilePath, outputDirectory);
            return File.Exists(vocalsPath) && File.Exists(accompanimentPath);
        }

        /// <summary>
        /// Filters out common warnings that don't indicate actual failures
        /// </summary>
        /// <param name="errorOutput">Raw error output from the process</param>
        /// <returns>Filtered error output with only genuine errors</returns>
        private string FilterWarnings(string errorOutput)
        {
            if (string.IsNullOrEmpty(errorOutput))
                return errorOutput;

            var lines = errorOutput.Split('\n');
            var filteredLines = new List<string>();

            foreach (var line in lines)
            {
                // Skip common warnings that don't indicate failure
                if (line.Contains("WARNING") ||
                    line.Contains("UserWarning") ||
                    line.Contains("FutureWarning") ||
                    line.Contains("DeprecationWarning") ||
                    line.Contains("deprecated") ||
                    line.Contains("ignore") ||
                    string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                filteredLines.Add(line);
            }

            return string.Join("\n", filteredLines);
        }

        /// <summary>
        /// Gets information about the audio separator setup
        /// </summary>
        /// <returns>String containing setup information</returns>
        public string GetSetupInfo()
        {
            var info = new List<string>();
            
            info.Add($"Python Path: {_pythonExecutablePath}");
            info.Add($"Script Path: {_audioSeparatorScriptPath}");
            info.Add($"Available: {_isAvailable}");
            
            if (!string.IsNullOrEmpty(_pythonExecutablePath))
            {
                if (_pythonExecutablePath == "python")
                {
                    info.Add("Python Exists: Available in PATH");
                }
                else
                {
                    info.Add($"Python Exists: {File.Exists(_pythonExecutablePath)}");
                }
            }
            
            if (!string.IsNullOrEmpty(_audioSeparatorScriptPath))
            {
                info.Add($"Script Exists: {File.Exists(_audioSeparatorScriptPath)}");
            }

            // Test the detection process
            try
            {
                var testProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _pythonExecutablePath,
                        Arguments = $"\"{_audioSeparatorScriptPath}\" --help",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                testProcess.Start();
                testProcess.WaitForExit(10000);
                
                var output = testProcess.StandardOutput.ReadToEnd();
                var error = testProcess.StandardError.ReadToEnd();
                
                info.Add($"Test Exit Code: {testProcess.ExitCode}");
                info.Add($"Test Output Contains 'Separate audio': {output.Contains("Separate audio")}");
                info.Add($"Test Output Contains 'vocals and accompaniment': {output.Contains("vocals and accompaniment")}");
                if (!string.IsNullOrEmpty(error))
                {
                    info.Add($"Test Error: {error}");
                }
            }
            catch (Exception ex)
            {
                info.Add($"Test Exception: {ex.Message}");
            }
            
            return string.Join("\n", info);
        }

        /// <summary>
        /// Sanitizes a filename by removing or replacing invalid characters
        /// </summary>
        /// <param name="fileName">The filename to sanitize</param>
        /// <returns>A sanitized filename safe for use in paths</returns>
        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unknown";

            // Remove or replace invalid path characters
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;
            
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }
            
            // Also replace some additional problematic characters
            sanitized = sanitized.Replace('(', '_')
                                 .Replace(')', '_')
                                 .Replace('[', '_')
                                 .Replace(']', '_')
                                 .Replace('{', '_')
                                 .Replace('}', '_')
                                 .Replace('&', '_')
                                 .Replace('%', '_')
                                 .Replace('$', '_')
                                 .Replace('#', '_')
                                 .Replace('@', '_')
                                 .Replace('!', '_');
            
            // Remove consecutive underscores and trim
            while (sanitized.Contains("__"))
            {
                sanitized = sanitized.Replace("__", "_");
            }
            
            sanitized = sanitized.Trim('_');
            
            // Ensure it's not empty and not too long
            if (string.IsNullOrEmpty(sanitized))
                sanitized = "audio_file";
            
            if (sanitized.Length > 30)
                sanitized = sanitized.Substring(0, 30).TrimEnd('_');
                
            return sanitized;
        }
    }
}
