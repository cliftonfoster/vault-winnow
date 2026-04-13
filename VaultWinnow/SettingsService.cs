using Newtonsoft.Json;
using System.IO;

namespace VaultWinnow;

public static class SettingsService
{
    private static readonly string AppFolder =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VaultWinnow");

    private static readonly string SettingsPath =
        Path.Combine(AppFolder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonConvert.DeserializeObject<AppSettings>(json);

            return settings ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppFolder);

            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Swallow for now: settings failure should not break the app.
        }
    }
}