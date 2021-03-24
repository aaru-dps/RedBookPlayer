using System;
using System.IO;
using System.Text.Json;

namespace RedBookPlayer
{
    public class Settings
    {
        public bool AutoPlay { get; set; } = false;
        public string SelectedTheme { get; set; } = "default";
        string filePath;

        public Settings() { }

        public Settings(string filePath)
        {
            this.filePath = filePath;
        }

        public static Settings Load(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    Settings settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(filePath));
                    settings.filePath = filePath;
                    Console.WriteLine(settings.AutoPlay);

                    MainWindow.ApplyTheme(settings.SelectedTheme);

                    return settings;
                }
                catch (JsonException)
                {
                    Console.WriteLine("Couldn't parse settings, reverting to default");
                    return new Settings(filePath);
                }
            }
            else
            {
                return new Settings(filePath);
            }
        }

        public void Save()
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
    }
}