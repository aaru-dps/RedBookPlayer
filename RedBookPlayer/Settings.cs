using System;
using System.IO;
using System.Text.Json;

namespace RedBookPlayer
{
    public class Settings
    {
        string filePath;

        public Settings() {}

        public Settings(string filePath) => this.filePath = filePath;

        public bool   AutoPlay               { get; set; }
        public bool   IndexButtonChangeTrack { get; set; }
        public bool   AllowSkipHiddenTrack   { get; set; }
        public int    Volume                 { get; set; } = 100;
        public string SelectedTheme          { get; set; } = "default";

        public static Settings Load(string filePath)
        {
            if(File.Exists(filePath))
            {
                try
                {
                    Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(filePath));
                    settings.filePath = filePath;

                    MainWindow.ApplyTheme(settings.SelectedTheme);

                    return settings;
                }
                catch(JsonException)
                {
                    Console.WriteLine("Couldn't parse settings, reverting to default");

                    return new Settings(filePath);
                }
            }

            return new Settings(filePath);
        }

        public void Save()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
    }
}