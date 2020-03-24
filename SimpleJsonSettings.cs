using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TylorsTech.SimpleJsonSettings
{
    public class NotInitializedException : Exception
    {
        public NotInitializedException(string message) : base(message)
        {
        }

        public NotInitializedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public NotInitializedException()
        {
        }
    }

    public static class SettingsManager
    {
        private static bool _hasBeenInitialized = false;
        private static string _settingsDirectory;

        public static void Initialize(string settingsDirectory)
        {
            if (!Directory.Exists(settingsDirectory))
                Directory.CreateDirectory(settingsDirectory);

            _settingsDirectory = settingsDirectory;
            _hasBeenInitialized = true;
        }

        /// <summary>
        /// Loads a SettingsFile from a specified location.
        /// </summary>
        /// <param name="fileName">Name of file stored in the settings directory.</param>
        /// <param name="throwOnFail">True to throw an exception on failure; false to return an empty SettingsFile.</param>
        public static async Task<SettingsFile> LoadAsync(string fileName, bool throwOnFail = false)
        {
            if (!_hasBeenInitialized)
                throw new NotInitializedException("Library has not been initialized!");

            var filePath = Path.Combine(_settingsDirectory, fileName);
            var fileExists = File.Exists(filePath);

            if (!fileExists && throwOnFail)
                throw new FileNotFoundException($"No settings file was located at path: {filePath}");

            if (!fileExists)
                return new SettingsFile();

            var settingsFile = new SettingsFile { ParsedObject = JObject.Parse(await ReadTextAsync(filePath)) };
            return settingsFile;
        }

        public static Task SaveAsync(string fileName, SettingsFile file)
        {
            if (!_hasBeenInitialized)
                throw new NotInitializedException("Library has not been initialized!");

            var filePath = Path.Combine(_settingsDirectory, fileName);
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            return WriteTextAsync(filePath, file.ParsedObject.ToString(Formatting.Indented));
        }

        /// <summary>
        /// Reads text from a file asynchronously.
        /// </summary>
        /// <param name="filePath">Path to file</param>
        private static async Task<string> ReadTextAsync(string filePath)
        {
            using (var sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                var sb = new StringBuilder();

                var buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    var text = Encoding.Unicode.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Writes text to a file asynchronously.
        /// </summary>
        /// <param name="filePath">Path to file</param>
        /// <param name="text">Text to write</param>
        private static async Task WriteTextAsync(string filePath, string text)
        {
            var encodedText = Encoding.Unicode.GetBytes(text);

            using (var sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }
    }

    public class SettingsFile
    {
        internal JObject ParsedObject;

        public T GetSetting<T>(string key) => GetSetting<T>(key, default(T));

        public T GetSetting<T>(string key, T defaultValue)
        {
            if (!ParsedObject.ContainsKey(key))
                return defaultValue;

            return ParsedObject.GetValue(key).ToObject<T>();
        }

        public void SetSetting(string key, object value)
        {
            ParsedObject[key] = JToken.FromObject(value);
        }
    }
}
