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
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // Try multiple possible locations for the script
            string[] possibleScriptPaths = {
                // First try the bin/Resources folder (where your manual test worked)
                Path.Combine(baseDir, "Resources", "audio_separator_wrapper.py"),
                // Then try going up to project root
                Path.Combine(Directory.GetParent(baseDir)?.Parent?.Parent?.FullName ?? "", "Resources", "audio_separator_wrapper.py"),
                // Fallback to any Resources folder we can find
                Path.Combine(Path.GetDirectoryName(baseDir) ?? "", "Resources", "audio_separator_wrapper.py")
            };
            
            _audioSeparatorScriptPath = "";
            foreach (var path in possibleScriptPaths)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    _audioSeparatorScriptPath = path;
                    break;
                }
            }
            
            // Check if both Python and the script are available
            _isAvailable = !string.IsNullOrEmpty(_pythonExecutablePath) && !string.IsNullOrEmpty(_audioSeparatorScriptPath);
            
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
        /// Separates a song into stems (vocals and accompaniment) using the test_fixed method
        /// </summary>
        /// <param name="inputFilePath">Path to the input audio file (can be anywhere on the system)</param>
        /// <returns>True if separation was successful, false otherwise</returns>
        public async Task<bool> SeparateAudioAsync(string inputFilePath)
        {
            try
            {
                // Check if the service is available
                if (!_isAvailable)
                {
                    ReportProgress("Audio separation is not available. Python or audio-separator package is missing.", 0, true, true);
                    return false;
                }

                // Validate input file exists
                if (!File.Exists(inputFilePath))
                {
                    ReportProgress($"Input file not found: {inputFilePath}", 0, true, true);
                    return false;
                }

                // Use test_fixed directory in Resources folder
                var resourcesDir = Path.GetDirectoryName(_audioSeparatorScriptPath);
                var outputDirectory = Path.Combine(resourcesDir!, "test_fixed");

                // Create test_fixed directory if it doesn't exist
                Directory.CreateDirectory(outputDirectory);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutablePath,
                    Arguments = $"\"{_audioSeparatorScriptPath}\" \"{inputFilePath}\" \"{outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_audioSeparatorScriptPath)
                };

                // Set environment variables to suppress warnings
                processStartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
                processStartInfo.EnvironmentVariables["PYTHONWARNINGS"] = "ignore";

                ReportProgress("Starting audio separation...", 5);
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

                    // Create a progress reporting task
                    var progressStartTime = DateTime.Now;
                    var progressReportingTask = Task.Run(async () =>
                    {
                        while (!process.HasExited)
                        {
                            await Task.Delay(10000);
                            if (!process.HasExited)
                            {
                                var elapsed = DateTime.Now - progressStartTime;
                                ReportProgress($"Processing... {elapsed.Minutes}m {elapsed.Seconds}s elapsed", null);
                            }
                        }
                    });

                    // Wait for the process to complete asynchronously with timeout
                    var timeoutTask = Task.Delay(1800000); // 30 minutes
                    var processTask = Task.Run(() => process.WaitForExit());
                    
                    var completedTask = await Task.WhenAny(processTask, timeoutTask);
                    
                    if (completedTask == timeoutTask)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                        ReportProgress("Audio separation process timed out after 30 minutes", 0, true, true);
                        return false;
                    }

                    await Task.WhenAll(outputTask, errorTask);

                    var output = string.Join("\n", outputLines);
                    var error = string.Join("\n", errorLines);

                    if (process.ExitCode == 0)
                    {
                        ReportProgress("Audio separation completed successfully", 90);
                        
                        // Verify that the expected output files were created
                        var (vocalsPath, accompanimentPath) = GetSeparatedFilePathsTestFixed(inputFilePath);
                        
                        // Debug logging for troubleshooting
                        ReportProgress($"Checking for files: Vocals={vocalsPath}, Accompaniment={accompanimentPath}", null);
                        
                        bool vocalsExists = File.Exists(vocalsPath);
                        bool accompanimentExists = File.Exists(accompanimentPath);
                        
                        ReportProgress($"File check results: Vocals={vocalsExists}, Accompaniment={accompanimentExists}", null);
                        
                        if (vocalsExists && accompanimentExists)
                        {
                            // Validate that the files are not empty
                            var vocalsSize = new FileInfo(vocalsPath).Length;
                            var accompanimentSize = new FileInfo(accompanimentPath).Length;
                            
                            if (vocalsSize == 0 || accompanimentSize == 0)
                            {
                                ReportProgress($"Audio separation completed but output files are empty. Vocals: {vocalsSize} bytes, Accompaniment: {accompanimentSize} bytes", 0, true, true);
                                return false;
                            }
                            
                            ReportProgress("Audio separation completed successfully!", 100, true);
                            return true;
                        }
                        else
                        {
                            // Provide detailed error information
                            var songDir = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(inputFilePath));
                            var detailedError = $"Audio separation completed but expected output files were not found.\n\n" +
                                              $"Expected directory: {songDir}\n" +
                                              $"Vocals file: {vocalsExists}\n" +
                                              $"Accompaniment file: {accompanimentExists}\n\n";
                            
                            if (Directory.Exists(songDir))
                            {
                                var actualFiles = Directory.GetFiles(songDir, "*.wav");
                                detailedError += $"Files actually found: {string.Join(", ", actualFiles.Select(Path.GetFileName))}\n\n";
                                
                                if (actualFiles.Length == 0)
                                {
                                    detailedError += "No separated audio files were created. This may indicate the audio format is not supported or the file is corrupted.";
                                }
                            }
                            else
                            {
                                detailedError += "Output directory was not created, indicating the separation process failed.";
                            }
                            
                            ReportProgress(detailedError, 0, true, true);
                            return false;
                        }
                    }
                    else
                    {
                        // Filter out common warnings that don't indicate failure
                        var filteredError = FilterWarnings(error);
                        
                        // Provide more detailed error information
                        var errorMessage = $"Audio separation process failed with exit code {process.ExitCode}";
                        
                        if (!string.IsNullOrEmpty(filteredError))
                        {
                            errorMessage += $":\n{filteredError}";
                        }
                        
                        // Add debugging information
                        errorMessage += $"\n\nDebug Information:";
                        errorMessage += $"\nInput file: {inputFilePath}";
                        errorMessage += $"\nOutput directory: {outputDirectory}";
                        errorMessage += $"\nWorking directory: {Path.GetDirectoryName(_audioSeparatorScriptPath)}";
                        
                        // Check if input file still exists and its size
                        if (File.Exists(inputFilePath))
                        {
                            var fileInfo = new FileInfo(inputFilePath);
                            errorMessage += $"\nInput file size: {fileInfo.Length} bytes";
                        }
                        else
                        {
                            errorMessage += $"\nInput file not found!";
                        }
                        
                        ReportProgress(errorMessage, 0, true, true);
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
        /// Separates a song into stems (vocals and accompaniment) with custom output directory
        /// </summary>
        /// <param name="inputFilePath">Path to the input audio file</param>
        /// <param name="outputDirectory">Custom output directory for the separated stems</param>
        /// <returns>True if separation was successful, false otherwise</returns>
        public async Task<bool> SeparateAudioAsync(string inputFilePath, string outputDirectory)
        {
            try
            {
                // Check if the service is available
                if (!_isAvailable)
                {
                    ReportProgress("Audio separation is not available. Python or audio-separator package is missing.", 0, true, true);
                    ReportProgress($"Python path: {_pythonExecutablePath}", null, false, false);
                    ReportProgress($"Script path: {_audioSeparatorScriptPath}", null, false, false);
                    ReportProgress($"Script exists: {File.Exists(_audioSeparatorScriptPath)}", null, false, false);
                    return false;
                }

                // Validate input file exists
                if (!File.Exists(inputFilePath))
                {
                    ReportProgress($"Input file not found: {inputFilePath}", 0, true, true);
                    return false;
                }

                // Create output directory if it doesn't exist
                Directory.CreateDirectory(outputDirectory);

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutablePath,
                    Arguments = $"\"{_audioSeparatorScriptPath}\" \"{inputFilePath}\" \"{outputDirectory}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(_audioSeparatorScriptPath) ?? Directory.GetCurrentDirectory()
                };

                // Set environment variables to suppress warnings
                processStartInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
                processStartInfo.EnvironmentVariables["PYTHONWARNINGS"] = "ignore";

                ReportProgress("Starting audio separation...", 5);
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

                    // Create a progress reporting task
                    var progressStartTime = DateTime.Now;
                    var progressReportingTask = Task.Run(async () =>
                    {
                        while (!process.HasExited)
                        {
                            await Task.Delay(10000);
                            if (!process.HasExited)
                            {
                                var elapsed = DateTime.Now - progressStartTime;
                                ReportProgress($"Processing... {elapsed.Minutes}m {elapsed.Seconds}s elapsed", null);
                            }
                        }
                    });

                    // Wait for process to complete
                    await process.WaitForExitAsync();
                    await Task.WhenAll(outputTask, errorTask);

                    var outputText = string.Join("\n", outputLines);
                    var errorText = string.Join("\n", errorLines);

                    if (process.ExitCode == 0)
                    {
                        // Check if output files actually exist
                        var (vocalsPath, accompanimentPath) = GetSeparatedFilePaths(inputFilePath, outputDirectory);
                        
                        if (File.Exists(vocalsPath) && File.Exists(accompanimentPath))
                        {
                            ReportProgress("Audio separation completed successfully!", 100, true);
                            return true;
                        }
                        else
                        {
                            ReportProgress("Process completed but output files were not created", 0, true, true);
                            return false;
                        }
                    }
                    else
                    {
                        var errorMessage = $"Audio separation failed with exit code {process.ExitCode}";
                        if (!string.IsNullOrEmpty(errorText))
                        {
                            errorMessage += $"\nError details: {FilterWarnings(errorText)}";
                        }
                        
                        errorMessage += $"\nOutput directory: {outputDirectory}";
                        errorMessage += $"\nWorking directory: {Path.GetDirectoryName(_audioSeparatorScriptPath)}";
                        
                        // Check if input file still exists and its size
                        if (File.Exists(inputFilePath))
                        {
                            var fileInfo = new FileInfo(inputFilePath);
                            errorMessage += $"\nInput file size: {fileInfo.Length} bytes";
                        }
                        else
                        {
                            errorMessage += $"\nInput file not found!";
                        }
                        
                        ReportProgress(errorMessage, 0, true, true);
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

        /// <summary>
        /// Gets the expected paths for separated stems in the test_fixed directory
        /// </summary>
        /// <param name="inputFilePath">Original audio file path</param>
        /// <returns>Tuple containing paths to vocals and accompaniment files</returns>
        public (string vocalsPath, string accompanimentPath) GetSeparatedFilePathsTestFixed(string inputFilePath)
        {
            var resourcesDir = Path.GetDirectoryName(_audioSeparatorScriptPath);
            var testFixedDir = Path.Combine(resourcesDir!, "test_fixed");
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
            
            // Use the same sanitization logic as the Python script to match the actual folder name
            var sanitizedFileName = SanitizePythonFilename(fileNameWithoutExtension);
            var stemDirectory = Path.Combine(testFixedDir, sanitizedFileName);
            
            var vocalsPath = Path.Combine(stemDirectory, "vocals.wav");
            var accompanimentPath = Path.Combine(stemDirectory, "accompaniment.wav");
            
            return (vocalsPath, accompanimentPath);
        }

        /// <summary>
        /// Sanitizes filename using the same logic as the Python script
        /// </summary>
        /// <param name="filename">The filename to sanitize</param>
        /// <returns>A sanitized filename that matches the Python script output</returns>
        private string SanitizePythonFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return "unknown";

            // Apply the same sanitization as Python script
            var sanitized = filename;
            
            // Remove or replace invalid characters (same as Python regex)
            sanitized = Regex.Replace(sanitized, @"[<>:""/\\|?*]", "_");
            sanitized = Regex.Replace(sanitized, @"[()[\]{}]", "_");
            sanitized = Regex.Replace(sanitized, @"[&%$#@!]", "_");
            
            // Remove consecutive underscores
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            
            // Remove leading/trailing underscores and limit length to 30 chars (same as Python)
            sanitized = sanitized.Trim('_');
            if (sanitized.Length > 30)
                sanitized = sanitized.Substring(0, 30).TrimEnd('_');
                
            return string.IsNullOrEmpty(sanitized) ? "audio_file" : sanitized;
        }

        /// <summary>
        /// Checks if separated stems already exist for a given audio file in test_fixed directory
        /// </summary>
        /// <param name="inputFilePath">Original audio file path</param>
        /// <returns>True if both stems exist, false otherwise</returns>
        public bool StemsExistTestFixed(string inputFilePath)
        {
            var (vocalsPath, accompanimentPath) = GetSeparatedFilePathsTestFixed(inputFilePath);
            return File.Exists(vocalsPath) && File.Exists(accompanimentPath);
        }
    }
}
