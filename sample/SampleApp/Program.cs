using Newtonsoft.Json;

using System;
using System.IO;
using System.Threading.Tasks;
using TylorsTech.SimpleJsonSettings;

namespace SampleApp
{
    class Program
    {     
        public void Main()
        {
            CreateKeyValueSettings();
            CreateStronglyTypedSettings();
        }

        public void CreateKeyValueSettings()
        {
            // Create a new KeyValueSettingsFile with default values
            var settingsBuilder = KeyValueSettingsFileBuilder.FromFile(Path.Combine(Environment.CurrentDirectory, "settings.json"))
                .WithDefault("isTest", true) // Supply a default value
                .LoadOrCreate(); // Load from file if it exists, or create a new version if it does not.

            //now actually create the SettingsFile object
            var settings = settingsBuilder.Build();

            //now edit a setting
            settings.SetSetting("canUseObjects", new { test = true });

            //why not add a different object?
            var someString = "This is a test!";
            settings.SetSetting("stringTest", someString);
            //Now the setting is updated. However...
            someString = "See? I've changed!";
            //This will not update in the settings file. You must make sure to again call
            settings.SetSetting("stringTest", someString);
            //Or the object will not be updated in the Settings file.

            //Save settings to their file.
            settings.Save();
        }

        public void CreateStronglyTypedSettings()
        {
            var settingsBuilder = StronglyTypedSettingsFileBuilder<ExampleSettingsDefinition>
                .FromFile(Environment.CurrentDirectory, "ExampleSettings.json"); // Overloaded to call Path.Combine() internally

            settingsBuilder.WithDefaultNullValueHandling(NullValueHandling.Include) // Include any property that is set to null in the output JSON
                .WithFileNotFoundBehavior(SettingsFileNotFoundBehavior.ReturnDefault) // Return a default settings file instead of null
                .WithDefaultValueHandling(DefaultValueHandling.Populate); // Populate object with default values if the settings are not found in the JSON, or if we call CreateDefault(). 
                            
            // If you want to create a default and save it, call:
            settingsBuilder.CreateDefault().Save();

            // Otherwise, if you want to load:
            var settings = settingsBuilder.Build();

            // Since we set FileNotFoundBehavior above, and also called CreateDefault(), one of those will have created a default version.
            // Since we DefaultValueHandling was set to populate, we should have a DefaultTrue property with the value set to True.
            bool shouldBeTrue = settings.DefaultTrue;
        }
    }

    public class ExampleSettingsDefinition : StronglyTypedSettingsDefinition
    {
        public string TestString { get; set; }

        // Don't include in the settings file:
        [Newtonsoft.Json.JsonIgnore]
        public bool DontIncludeMe { get; set; }

        // Specify a default value:
        // This default value will be used when deserializing, or if CreateDefault() is called when the serializer's settings are set to Populate.
        [System.ComponentModel.DefaultValue(true)]
        public bool DefaultTrue { get; set; }
    }
}
