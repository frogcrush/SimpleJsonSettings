using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;

using TylorsTech.SimpleJsonSettings.Internal;

namespace TylorsTech.SimpleJsonSettings
{
    public abstract class StronglyTypedSettingsDefinition : ISettingsFileBase
    {
        [JsonIgnore]
        internal string _fileLocation;

        [JsonIgnore]
        protected internal JsonSerializerSettings _serializerSettings;
        
        public virtual bool Exists()
        {
            return File.Exists(_fileLocation);
        }

        public virtual void Save()
        {
            File.WriteAllText(_fileLocation, JsonConvert.SerializeObject(this, _serializerSettings));
        }
    }

    public class StronglyTypedSettingsFileBuilder<TSettingsType> where TSettingsType : StronglyTypedSettingsDefinition
    {
        private JsonSerializerSettings _serializerSettings;
        private string _fileLocation;

        private SettingsFileNotFoundBehavior _notFoundBehavior = SettingsFileNotFoundBehavior.ReturnNull;
                
        public StronglyTypedSettingsFileBuilder()
        {
            _serializerSettings = new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Populate,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented
            };
        }

        /// <summary>
        /// Specifies the file location of a <see cref="TSettingsType"/>.
        /// </summary>
        public static StronglyTypedSettingsFileBuilder<TSettingsType> FromFile(string fileName)
        {
            return new StronglyTypedSettingsFileBuilder<TSettingsType>() { _fileLocation = fileName };
        }

        /// <summary>
        /// Loads a settings file, combining all supplied values into a single file path.
        /// </summary>
        public static StronglyTypedSettingsFileBuilder<TSettingsType> FromFile(params string[] path)
            => FromFile(Path.Combine(path));

        public StronglyTypedSettingsFileBuilder<TSettingsType> WithSerializerSettings(JsonSerializerSettings settings)
        {
            _serializerSettings = settings;
            return this;
        }

        public StronglyTypedSettingsFileBuilder<TSettingsType> WithDefaultValueHandling(DefaultValueHandling value)
        {
            _serializerSettings.DefaultValueHandling = value;
            return this;
        }

        public StronglyTypedSettingsFileBuilder<TSettingsType> WithDefaultNullValueHandling(NullValueHandling value)
        {
            _serializerSettings.NullValueHandling = value;
            return this;
        }

        /// <summary>
        /// You should not call any With methods before this method.
        /// </summary>
        public StronglyTypedSettingsFileBuilder<TSettingsType> WithJsonConverter(JsonConverter converter)
        {
            _serializerSettings.Converters.Add(converter);
            return this;
        }

        /// <summary>
        /// Creates an instance of <see cref="TSettingsType"/>. If DefaultValueHandling is set to Populate or IgnoreAndPopulate, <see cref="DefaultValueAttribute"/> values are set.
        /// </summary>
        public TSettingsType CreateDefault()
        {
            var obj = Activator.CreateInstance<TSettingsType>();

            obj._fileLocation = _fileLocation;
            obj._serializerSettings = _serializerSettings;

            if (_serializerSettings.DefaultValueHandling == DefaultValueHandling.Populate || _serializerSettings.DefaultValueHandling == DefaultValueHandling.IgnoreAndPopulate)
            {
                foreach (var propDesc in obj.GetType().GetProperties())
                {
                    var defValAttr = propDesc.GetCustomAttribute<DefaultValueAttribute>();
                    if (defValAttr != null)
                    {
                        propDesc.SetValue(obj, defValAttr.Value);
                    }
                }
            }

            return obj;
        }

        /// <summary>
        /// Specifies the behavior to use when the JSON file does not exist.
        /// </summary>
        public StronglyTypedSettingsFileBuilder<TSettingsType> WithFileNotFoundBehavior(SettingsFileNotFoundBehavior value)
        {
            _notFoundBehavior = value;
            return this;
        }

        /// <summary>
        /// Loads settings from a file, or if <see cref="SettingsFileNotFoundBehavior"/> is <see cref="SettingsFileNotFoundBehavior.ReturnDefault"/>, returns a default settings file.
        /// </summary>
        /// <returns>
        /// Returns a new <see cref="TSettingsType"/>, or one of two values if no settings file exists:
        /// If <see cref="SettingsFileNotFoundBehavior"/> is <see cref="SettingsFileNotFoundBehavior.ReturnDefault"/>, returns a default settings file.
        ///  if <see cref="SettingsFileNotFoundBehavior"/> is <see cref="SettingsFileNotFoundBehavior.ReturnNull"/>, returns null. 
        /// </returns>
        public TSettingsType Build()
        {
            if (!File.Exists(_fileLocation))
            {
                if (_notFoundBehavior == SettingsFileNotFoundBehavior.ReturnDefault)
                {
                    return CreateDefault();
                }
                else
                {
                    return null;
                }
            }

            var obj = JsonConvert.DeserializeObject<TSettingsType>(File.ReadAllText(_fileLocation), _serializerSettings);
            obj._fileLocation = _fileLocation;
            obj._serializerSettings = _serializerSettings;
            return obj;
        }
    }

    public enum SettingsFileNotFoundBehavior
    {
        ReturnNull,
        ReturnDefault
    }
}
