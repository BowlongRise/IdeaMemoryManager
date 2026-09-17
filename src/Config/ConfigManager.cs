using System;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using IdeaMemoryManager.Common;
using IdeaMemoryManager.Common.Localization;

namespace IdeaMemoryManager.Config
{
    /// <summary>
    /// Manages persistent application configuration stored in the user's AppData directory.
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "IdeaMemoryManager");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "IdeaMemoryManager";

        public static AppSettings Current { get; private set; } = new AppSettings();

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                    I18n.CurrentLanguage = Current.Language;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load settings file, using defaults", ex);
                Current = new AppSettings();
            }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                Current.Language = I18n.CurrentLanguage;
                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to persist settings", ex);
            }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\" --minimized");
                    }
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
                Current.AutoStartWithWindows = enable;
                Save();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update Windows startup registry entry", ex);
            }
        }

        public static void RecordSavedBytes(long bytes)
        {
            if (bytes > 0)
            {
                Current.TotalSavedBytes += bytes;
                Current.TotalCleanCount++;
                Save();
            }
        }
    }
}