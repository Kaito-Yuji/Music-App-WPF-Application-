using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WpfMusicPlayer.Models;

namespace WpfMusicPlayer.Services
{
    /// <summary>
    /// Shared plumbing for the Python-backed features. Finds the interpreter once and runs
    /// scripts while draining both pipes, which the separation and mood features both rely on.
    /// </summary>
    public class PythonRunner
    {
        /// <summary>Exit code the probe script uses to report that FFmpeg is missing.</summary>
        private const int FfmpegMissingExitCode = 3;

        private readonly Lazy<string> _executable;

        public PythonRunner()
        {
            // Probing costs up to five seconds, so do not pay it during startup
            _executable = new Lazy<string>(FindPython);
        }

        public bool IsAvailable => !string.IsNullOrEmpty(_executable.Value);

        public string Executable => _executable.Value;

        private static string FindPython()
        {
            try
            {
                using var process = new Process
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

                if (process.HasExited && process.ExitCode == 0)
                    return "python";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Python probe failed: {ex.Message}");
            }

            return "";
        }

        /// <summary>
        /// Runs the interpreter and returns once it exits. Both pipes are drained as the process
        /// runs: if an OS pipe buffer fills up, Python blocks on its next print and never
        /// finishes.
        /// </summary>
        /// <param name="onOutput">Called for every non-empty stdout line, on a background thread.</param>
        public async Task<PythonResult> RunAsync(string arguments, CancellationToken cancellationToken = default,
            Action<string>? onOutput = null)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            var standardOutput = new StringBuilder();
            var standardError = new StringBuilder();

            process.OutputDataReceived += (_, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.Data))
                    return;

                var line = args.Data.Trim();
                standardOutput.AppendLine(line);
                onOutput?.Invoke(line);
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    standardError.AppendLine(args.Data.Trim());
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            return new PythonResult(process.ExitCode, standardOutput.ToString(), standardError.ToString());
        }

        /// <summary>
        /// Confirms the interpreter can import every module in <paramref name="modules"/>, and
        /// optionally that FFmpeg is on PATH, so the user is told exactly what is missing instead
        /// of watching a feature fail silently.
        /// </summary>
        public async Task<DependencyCheck> CheckModulesAsync(IEnumerable<string> modules, bool requireFfmpeg)
        {
            if (!IsAvailable)
            {
                return new DependencyCheck(false,
                    "Python was not found.\n\n" +
                    "Install Python 3.12 from python.org and tick \"Add python.exe to PATH\" during setup. " +
                    "If Windows opens the Microsoft Store instead, turn off the python.exe alias under " +
                    "Settings > Apps > Advanced app settings > App execution aliases.");
            }

            var imports = string.Join(" ", modules.Select(m => $"import {m};"));
            var ffmpegCheck = requireFfmpeg
                ? $"sys.exit(0 if shutil.which('ffmpeg') else {FfmpegMissingExitCode})"
                : "sys.exit(0)";

            // The interpreter path is printed before the imports so it is still reported when one
            // of them fails. On a machine with several Python installations, knowing which one
            // was used is usually the whole answer.
            var probe = $"import shutil, sys; print(sys.executable); {imports} {ffmpegCheck}";

            var result = await RunAsync($"-c \"{probe}\"");
            var interpreter = result.StandardOutput
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? "python";

            return result.ExitCode switch
            {
                0 => new DependencyCheck(true, "Ready."),
                FfmpegMissingExitCode => new DependencyCheck(false,
                    "FFmpeg was not found.\n\nInstall it with:\n    winget install --id Gyan.FFmpeg -e\n\n" +
                    "Then open a new terminal and restart this app."),
                _ => new DependencyCheck(false, DescribeMissingModule(result.StandardError, interpreter))
            };
        }

        private static string DescribeMissingModule(string standardError, string interpreter)
        {
            var match = Regex.Match(standardError, @"No module named '([^']+)'");

            if (!match.Success)
            {
                return string.IsNullOrWhiteSpace(standardError)
                    ? "The Python environment could not be verified."
                    : $"The Python environment could not be verified:\n\n{standardError.Trim()}";
            }

            var module = match.Groups[1].Value;

            // The import name and the package name differ for some of these
            var package = module switch
            {
                "audio_separator" => "audio-separator",
                "sklearn" => "scikit-learn",
                _ => module
            };

            return $"The Python package '{package}' is not installed.\n\n" +
                   $"Install it with:\n    pip install {package}\n\n" +
                   $"The app is using this interpreter:\n    {interpreter}\n\n" +
                   "If that is not the Python you installed the package into, the package went to a " +
                   "different installation. Install it for the interpreter above, or fix the order of " +
                   "your PATH, then restart this app.";
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Could not stop the Python process: {ex.Message}");
            }
        }
    }
}
