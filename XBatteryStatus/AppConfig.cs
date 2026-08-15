using System;
using System.IO;
using System.Text.Json;

namespace XBatteryStatus
{
    /// <summary>
    /// Application settings stored as JSON in the config folder next to the exe,
    /// so no files are written outside the exe directory.
    /// </summary>
    public static class AppConfig
    {
        private static readonly string configDir = Path.Combine(AppContext.BaseDirectory, "config");
        private static readonly string settingsPath = Path.Combine(configDir, "settings.json");

        public static string ConfigDir => configDir;

        public static int Theme { get; set; } = 0;
        public static int Language { get; set; } = 0;
        public static int PollInterval { get; set; } = 15;
        public static string PopupTitle { get; set; } = "";
        public static string PopupSubtitle { get; set; } = "";
        public static string PopupTitleFontFamily { get; set; } = "Segoe UI Variable Text";
        public static float PopupTitleFontSize { get; set; } = 15.5f;
        public static bool PopupTitleFontBold { get; set; } = true;
        public static string PopupSubFontFamily { get; set; } = "Segoe UI Variable Text";
        public static float PopupSubFontSize { get; set; } = 12.5f;
        public static bool PopupSubFontBold { get; set; } = false;
        public static float PopupScale { get; set; } = 1f;
        public static bool PopupPosSet { get; set; } = false;
        public static int PopupPosX { get; set; } = 0;
        public static int PopupPosY { get; set; } = 0;
        public static string Sound { get; set; } = "";
        public static bool Startup { get; set; } = true;
        public static bool Logging { get; set; } = false;

        public static void Load()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    var data = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(settingsPath));
                    if (data == null) return;

                    Theme = data.Theme;
                    Language = data.Language;
                    PollInterval = Math.Max(3, data.PollInterval);
                    PopupTitle = data.PopupTitle ?? "";
                    PopupSubtitle = data.PopupSubtitle ?? "";
                    PopupTitleFontFamily = string.IsNullOrEmpty(data.PopupTitleFontFamily) ? "Segoe UI Variable Text" : data.PopupTitleFontFamily;
                    PopupTitleFontSize = data.PopupTitleFontSize <= 0 ? 15.5f : data.PopupTitleFontSize;
                    PopupTitleFontBold = data.PopupTitleFontBold;
                    PopupSubFontFamily = string.IsNullOrEmpty(data.PopupSubFontFamily) ? "Segoe UI Variable Text" : data.PopupSubFontFamily;
                    PopupSubFontSize = data.PopupSubFontSize <= 0 ? 12.5f : data.PopupSubFontSize;
                    PopupSubFontBold = data.PopupSubFontBold;
                    PopupScale = data.PopupScale <= 0 ? 1f : data.PopupScale;
                    PopupPosSet = data.PopupPosSet;
                    PopupPosX = data.PopupPosX;
                    PopupPosY = data.PopupPosY;
                    Sound = data.Sound ?? "";
                    Startup = data.Startup;
                    Logging = data.Logging;
                }
            }
            catch
            {
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(configDir))
                {
                    Directory.CreateDirectory(configDir);
                }

                var data = new ConfigData
                {
                    Theme = Theme,
                    Language = Language,
                    PollInterval = PollInterval,
                    PopupTitle = PopupTitle,
                    PopupSubtitle = PopupSubtitle,
                    PopupTitleFontFamily = PopupTitleFontFamily,
                    PopupTitleFontSize = PopupTitleFontSize,
                    PopupTitleFontBold = PopupTitleFontBold,
                    PopupSubFontFamily = PopupSubFontFamily,
                    PopupSubFontSize = PopupSubFontSize,
                    PopupSubFontBold = PopupSubFontBold,
                    PopupScale = PopupScale,
                    PopupPosSet = PopupPosSet,
                    PopupPosX = PopupPosX,
                    PopupPosY = PopupPosY,
                    Sound = Sound,
                    Startup = Startup,
                    Logging = Logging
                };
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(data));
            }
            catch
            {
            }
        }

        private class ConfigData
        {
            public int Theme { get; set; }
            public int Language { get; set; }
            public int PollInterval { get; set; } = 15;
            public string PopupTitle { get; set; } = "";
            public string PopupSubtitle { get; set; } = "";
            public string PopupTitleFontFamily { get; set; } = "Segoe UI Variable Text";
            public float PopupTitleFontSize { get; set; } = 15.5f;
            public bool PopupTitleFontBold { get; set; } = true;
            public string PopupSubFontFamily { get; set; } = "Segoe UI Variable Text";
            public float PopupSubFontSize { get; set; } = 12.5f;
            public bool PopupSubFontBold { get; set; } = false;
            public float PopupScale { get; set; } = 1f;
            public bool PopupPosSet { get; set; } = false;
            public int PopupPosX { get; set; } = 0;
            public int PopupPosY { get; set; } = 0;
            public string Sound { get; set; } = "";
            public bool Startup { get; set; } = true;
            public bool Logging { get; set; } = false;
        }
    }
}
