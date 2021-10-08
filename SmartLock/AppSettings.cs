using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace SmartLock
{
    class AppSettingsProperties
    {
        public bool ReplaceOriginalFile { get; set; }
    }
    static class AppSettings
    {
        public const string RegistryAssiciationEntry = "SmartLock";
        public const string EncryptedFileExtention = ".sml";
        private const string fileName = "app.json";
        private static string appPath;
        private static string settingsPath;
        private static AppSettingsProperties properties;
        public static AppSettingsProperties Properties
        {
            get
            {
                if (properties == null) Load();
                return properties;
            }
        }
        public static string ExecutablePath
        {
            get
            {
                if (appPath == null || appPath == "")
                {
                    appPath = Assembly.GetExecutingAssembly().Location;
                    if (appPath.EndsWith(".dll"))
                    {
                        appPath = appPath.Substring(0, appPath.Length - 4) + ".exe";
                    }
                }
                return appPath;
            }
        }
        public static string IconPath
        {
            get
            {
                return Path.GetDirectoryName(appPath) + "\\" + EncryptedFileExtention.Substring(1,EncryptedFileExtention.Length - 1) + ".ico";
            }
        }
        private static void Load()
        {//Application settings constructor
            settingsPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + fileName;
            if (File.Exists(settingsPath))
            {
                string json = File.ReadAllText(settingsPath);
                properties = JsonSerializer.Deserialize<AppSettingsProperties>(json);
            }
            else
            {
                //Set default values :
                properties = new AppSettingsProperties()
                {
                    ReplaceOriginalFile = false,
                };
            }
        }
        public static void Save()
        {
            try
            {
                string JSON = JsonSerializer.Serialize<AppSettingsProperties>(properties);
                File.WriteAllText(settingsPath, JSON);
            }
            catch (Exception)
            {
                //Probably write permition is missing
            }
        }
    }
}