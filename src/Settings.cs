using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoLayoutSwitch
{
    public class Settings
    {
        public bool PlaySound { get; set; } = true;
        public bool AutoStart { get; set; } = false;
        public int HotKeyVk { get; set; } = 0x7B; // VK_F12
        public uint HotKeyModifiers { get; set; } = 0x0004; // MOD_SHIFT
        public HashSet<string> Exceptions { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
        { 
            "hmm", "shh", "brr", "grr", "pfft", "psst", "tsk", "zzz", 
            "php", "jpg", "png", "gif", "xml", "html", "css", "sql", 
            "str", "std", "tmp", "msg", "ptr", "cmd", "cwd", "pwd", "src", "dst", "obj", "bin", "lib", "usr", "etc", 
            "dev", "sys", "log", "cfg", "ini", "url", "api", "sdk", "ide", "gui", "cli", "ssh", "ssl", "ftp", "tcp", "udp", "dns", "http", "www"
        };

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "AutoLayoutSwitch", 
            "settings.json");

        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null) return settings;
                }
            }
            catch { }
            
            var defaultSettings = new Settings();
            defaultSettings.Save(); // Create default file if not exists
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath)!;
                Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
