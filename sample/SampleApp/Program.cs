using System;
using System.IO;
using System.Threading.Tasks;
using TylorsTech.SimpleJsonSettings;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().MainAsync().ConfigureAwait(true);
        }

        public async Task MainAsync()
        {
            //Create a new SettingsFile with default values
            var settingsBuilder = SettingsFile.FromFile(Path.Combine(Environment.CurrentDirectory, "settings.json"))
                .WithDefault("isTest", true); //supply a default value
            await settingsBuilder.EnsureCreatedAsync(true); //Creates the file with default values if it does not already exist. If it does, this call will do nothing.
            await settingsBuilder.LoadIfExistsAsync(); //Load any existing settings

            //now actually create the SettingsFile object
            var settings = settingsBuilder.Create();

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
            await settings.SaveAsync();
        }
    }
}
