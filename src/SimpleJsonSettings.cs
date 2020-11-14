using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TylorsTech.SimpleJsonSettings;
using TylorsTech.SimpleJsonSettings.Internal;

namespace TylorsTech.SimpleJsonSettings
{
    namespace Internal
    {
        public interface ISettingsFileBase
        {
            bool Exists();
            void Save();
        }

        public interface IKeyValueSettingsFileBase : ISettingsFileBase
        {
            T GetSetting<T>(string key);
            string GetString(string key);
            bool GetBool(string key);

            //Setters
            void SetSetting(string key, object value);
            void SetSetting(string key, JToken value);

            bool ContainsKey(string key);
            bool Load(bool throwOnFail = false);
        }

        public abstract class BaseKeyValueSettingsFile : IKeyValueSettingsFileBase
        {
            internal JObject ParsedObject = new JObject();
            internal string _fileLocation;

            public object this[string key]
            {
                get => GetSetting<object>(key);
                set => SetSetting(key, value);
            }

            public abstract T GetSetting<T>(string key);

            public virtual string GetString(string key) => GetSetting<string>(key);

            public virtual bool GetBool(string key) => GetSetting<bool>(key);

            public virtual void SetSetting(string key, JToken value)
            {
                ParsedObject[key] = value;
            }

            public virtual void SetSetting(string key, object value) =>
                SetSetting(key, JToken.FromObject(value));

            public bool ContainsKey(string key) => ParsedObject.ContainsKey(key);

            protected BaseKeyValueSettingsFile(string fileLocation)
            {
                this._fileLocation = fileLocation;
            }

            protected BaseKeyValueSettingsFile(string folderPath, string fileName) : this(Path.Combine(folderPath, fileName))
            {
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);
            }

            public bool Exists() => File.Exists(_fileLocation);

            /// <summary>
            /// Loads a SettingsFile from a specified location.
            /// </summary>
            /// <param name="fileName">Path to settings file</param>
            /// <param name="throwOnFail">True to throw an exception on failure; false to return an empty SettingsFile.</param>
            public bool Load(bool throwOnFail = false)
            {
                var fileExists = File.Exists(_fileLocation);

                if (!fileExists && throwOnFail)
                    throw new FileNotFoundException($"No settings file was located at path: {_fileLocation}");

                if (!fileExists)
                    return false;

                var text = File.ReadAllText(_fileLocation);
                ParsedObject = JObject.Parse(text);
                return true;
            }

            public void Save()
            {
                File.WriteAllText(_fileLocation, ParsedObject.ToString(Formatting.Indented));
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
                    FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                };
            }
        }
    }

    /// <summary>
    /// A simple settings file, without additional helper methods or default values. 
    /// </summary>
    public class BasicSettingsFile : BaseKeyValueSettingsFile
    {
        public override T GetSetting<T>(string key) => GetSetting<T>(key, default(T));

        public virtual T GetSetting<T>(string key, T defaultValue)
        {
            if (!ParsedObject.ContainsKey(key))
                return defaultValue;

            return ParsedObject.GetValue(key).ToObject<T>();
        }

        public BasicSettingsFile(string fileLocation) : base(fileLocation)
        {
        }

        public BasicSettingsFile(string folderPath, string fileName) : base(folderPath, fileName)
        {
        }
    }

    /// <summary>
    /// A slightly more advanced settings file, with default value support
    /// </summary>
    public class KeyValueSettingsFile : BaseKeyValueSettingsFile, ISettingsFile
    {
        internal Dictionary<string, JToken> DefaultValues { get; } = new Dictionary<string, JToken>();

        public override T GetSetting<T>(string key)
        {
            if (ParsedObject.ContainsKey(key))
                return ParsedObject.GetValue(key).ToObject<T>();
            if (DefaultValues.ContainsKey(key))
                return DefaultValues[key].ToObject<T>();
            throw new Exception($"No entry or default entry was found with key {key}.");
        }

        public void AddDefaultValue(string key, JToken value)
        {
            if (DefaultValues.ContainsKey(key))
                throw new Exception("Value with that key already exists.");
            DefaultValues.Add(key, value);
        }

        public void AddDefaultValue(string key, object value) => AddDefaultValue(key, JToken.FromObject(value));

        public void RemoveDefaultValue(string key) => DefaultValues.Remove(key);

        public KeyValueSettingsFile(string fileLocation) : base(fileLocation)
        {
        }

        public KeyValueSettingsFile(string folderPath, string fileName) : base(folderPath, fileName)
        {
        }
    }

    public interface ISettingsFile : IKeyValueSettingsFileBase
    {
        void AddDefaultValue(string key, JToken value);
        void AddDefaultValue(string key, object value);
        void RemoveDefaultValue(string key);

    }

    public class KeyValueSettingsFileBuilder : KeyValueSettingsFileBuilder<KeyValueSettingsFile> {}

    public class KeyValueSettingsFileBuilder<TsettingsType> where TsettingsType : KeyValueSettingsFile
    {
        private TsettingsType _value;

        internal KeyValueSettingsFileBuilder() { }

        /// <summary>
        /// Creates a SettingsFile object for a specified JSON file path. Does not load data.
        /// </summary>
        /// <param name="filePath">Path to JSON file, which may not exist</param>
        public static KeyValueSettingsFileBuilder<TsettingsType> FromFile(string filePath)
        {
            var value = new KeyValueSettingsFileBuilder<TsettingsType>
            {
                _value = (TsettingsType)Activator.CreateInstance(typeof(TsettingsType), filePath)
            };
            
            return value;
        }

        /// <summary>
        /// Loads the SettingsFile from the file, failing silently if it does not exist.
        /// If you do not want to load failing silently, use <see cref="KeyValueSettingsFile.Load"/>
        /// </summary>
        /// <returns></returns>
        public KeyValueSettingsFileBuilder<TsettingsType> LoadIfExists()
        {
            _value.Load();
            return this;
        }

        /// <summary>
        /// Specify a default value for a given key
        /// </summary>
        public KeyValueSettingsFileBuilder<TsettingsType> WithDefault(string key, JToken token)
        {
            _value.AddDefaultValue(key, token);
            return this;
        }

        public KeyValueSettingsFileBuilder<TsettingsType> WithDefault<TValue>(string key, TValue value) => WithDefault(key, JToken.FromObject(value));

        public KeyValueSettingsFileBuilder<TsettingsType> WithDefault(string key, string value) => WithDefault<string>(key, value);

        public KeyValueSettingsFileBuilder<TsettingsType> WithDefault(string key, bool value) => WithDefault<bool>(key, value);

        /// <summary>
        /// Specify multiple default values
        /// </summary>
        public KeyValueSettingsFileBuilder<TsettingsType> WithDefaults(IEnumerable<KeyValuePair<string, JToken>> defaultValues)
        {
            foreach (var item in defaultValues)
                _value.AddDefaultValue(item.Key, item.Value);
            return this;
        }

        /// <summary>
        /// Specify multiple default values
        /// </summary>
        public KeyValueSettingsFileBuilder<TsettingsType> WithDefaults(IEnumerable<KeyValuePair<string, object>> defaultValues)
        {
            foreach (var item in defaultValues)
                _value.AddDefaultValue(item.Key, JToken.FromObject(item.Value));
            return this;
        }
        
        /// <summary>
        /// Ensure the relevant JSON file gets created.
        /// </summary>
        /// <param name="useDefaultValues">Write default values to file?</param>
        /// <returns></returns>
        public KeyValueSettingsFileBuilder<TsettingsType> EnsureCreated(bool useDefaultValues = false)
        {
            if (!File.Exists(_value._fileLocation) && useDefaultValues)
            {
                foreach (var defaultValue in _value.DefaultValues.Where(defaultValue => !_value.ContainsKey(defaultValue.Key))) //don't overwrite existing settings
                    _value.SetSetting(defaultValue.Key, defaultValue.Value);
            }
            
            _value.Save();
            return this;
        }

        public KeyValueSettingsFileBuilder<TsettingsType> LoadOrCreate() => LoadIfExists().EnsureCreated();

        public TsettingsType Build()
        {
            return _value;
        }
    }
}
