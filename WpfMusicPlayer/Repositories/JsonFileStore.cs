using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WpfMusicPlayer.Repositories
{
    /// <summary>
    /// Reads and writes one JSON file under the app's data folder.
    ///
    /// Every repository persists the same way, so path handling, serializer settings and the rule
    /// that a damaged file must never stop the app from starting all live here once instead of
    /// being repeated four times.
    /// </summary>
    public class JsonFileStore<T> where T : class, new()
    {
        private static readonly JsonSerializerOptions DefaultOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly JsonSerializerOptions _options;

        /// <param name="fileName">File name inside the data directory, e.g. "playlists.json".</param>
        /// <param name="directory">
        /// Overrides the default location. Handy for keeping a separate set of data.
        /// </param>
        public JsonFileStore(string fileName, string? directory = null, JsonSerializerOptions? options = null)
        {
            Directory = directory ?? DefaultDataDirectory;
            System.IO.Directory.CreateDirectory(Directory);

            FilePath = Path.Combine(Directory, fileName);
            _options = options ?? DefaultOptions;
        }

        /// <summary><c>%APPDATA%\WpfMusicPlayer</c>.</summary>
        public static string DefaultDataDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WpfMusicPlayer");

        public string Directory { get; }

        public string FilePath { get; }

        /// <summary>
        /// Returns the stored value, or a fresh empty one when the file is missing or unreadable.
        /// A corrupt file costs the listener their history, not their ability to open the app.
        /// </summary>
        public T Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new T();

                var json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new T();

                return JsonSerializer.Deserialize<T>(json, _options) ?? new T();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading '{FilePath}': {ex.Message}");
                return new T();
            }
        }

        public void Save(T value)
        {
            try
            {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(value, _options));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving '{FilePath}': {ex.Message}");
            }
        }
    }
}
