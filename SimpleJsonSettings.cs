using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TylorsTech.SimpleJsonSettings
{
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
        /// <returns></returns>
        public static async Task<SettingsFile> LoadAsync(string fileName, bool throwOnFail = false)
        {
            var filePath = Path.Combine(_settingsDirectory, fileName);
            var fileExists = File.Exists(filePath);

            if (!fileExists && throwOnFail)
                throw new FileNotFoundException($"No settings file was located at path: {filePath}");
            if (!fileExists)
                return new SettingsFile();

            using (var fileReader = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            {
                var settingsFile = new SettingsFile();
                settingsFile.ParsedObject = JObject.Parse(await ReadTextAsync(filePath));
                return settingsFile;
            }
        }

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

        public T GetSetting<T>(string key)
        {
            return ParsedObject.GetValue(key).ToObject<T>();
        }

        public void SetSetting(string key, object value)
        {
            ParsedObject[key] = JToken.FromObject(value);
        }
    }
}
