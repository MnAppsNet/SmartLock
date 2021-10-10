using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace SmartLock
{
    class AppSettingsProperties
    {
        public bool ReplaceOriginalFile { get; set; }
    }
    class AppTexts
    {
        public const string COMMAND_F = "COMMAND_F";
        public const string COMMAND_T = "COMMAND_T";
        public const string COMMAND_D = "COMMAND_D";
        public const string COMMAND_H = "COMMAND_H";
        public const string PRESS_ENTER_TO_EXIT = "PRESS_ENTER_TO_EXIT";
        public const string PRESS_ENTER_TO_CONTINUE = "PRESS_ENTER_TO_CONTINUE";
        public const string OPTIONS = "OPTIONS";
        public const string TRANSFORM_FILE = "TRANSFORM_FILE";
        public const string TRANSFORM_STRING = "TRANSFORM_STRING";
        public const string DECRYPT_WITH_PASS = "DECRYPT_WITH_PASS";
        public const string SETTINGS = "SETTINGS";
        public const string EXIT = "EXIT";
        public const string SELECT_OPTION = "SELECT_OPTION";
        public const string ENABLE = "ENABLE";
        public const string DISABLE = "DISABLE";
        public const string CONTEXT_MENU = "CONTEXT_MENU";
        public const string DONT_REPLACE = "DONT_REPLACE";
        public const string REPLACE = "REPLACE";
        public const string FILE_AFTER_TRANSFORMATION = "FILE_AFTER_TRANSFORMATION";
        public const string DISSOCIATE = "DISSOCIATE";
        public const string ASSSOCIATE = "ASSSOCIATE";
        public const string FILE_EXTENSION = "FILE_EXTENSION";
        public const string BACK = "BACK";
        public const string NOT_AUTHORIZED = "NOT_AUTHORIZED";
        public const string INITIALIZING = "INITIALIZING";
        public const string INITIALIZING_ERROR = "INITIALIZING_ERROR";
        public const string WINDOWS_HELLO_NOT_SUPPORTED = "WINDOWS_HELLO_NOT_SUPPORTED";
        public const string INPUT_NEW_PASSWORD = "INPUT_NEW_PASSWORD";
        public const string INPUT_PASSWORD = "INPUT_PASSWORD";
        public const string INPUT_PASSWORD_AGAIN = "INPUT_PASSWORD_AGAIN";
        public const string INPUT_FILE_OR_STRING = "INPUT_FILE_OR_STRING";
        public const string INPUT_STRING = "INPUT_STRING";
        public const string INPUT_FILE = "INPUT_FILE";
        public const string FILE_NOT_EXIST = "FILE_NOT_EXIST";
        public const string ENCRYPTED_STRING = "ENCRYPTED_STRING";
        public const string DECRYPTED_STRING = "DECRYPTED_STRING";
        public const string START_OF_STRING = "START_OF_STRING";
        public const string END_OF_STRING = "END_OF_STRING";
        public const string FILE_DECRYPTED = "FILE_DECRYPTED";

        public string language { get; set; }
        public Dictionary<string, Dictionary<string, string>> strings { get; set; }

        public string Get(string text)
        {
            return strings[language][text];
        }
    }
    static class AppSettings
    {
        public const string RegistryAssiciationEntry = "SmartLock";
        public const string EncryptedFileExtention = ".sml";
        private const string settingsFileName = "app.json";
        private const string textsFileName = "texts.json";
        private static string appPath;
        private static AppSettingsProperties properties;
        public static AppSettingsProperties Properties
        {
            get
            {
                if (properties == null) Load();
                return properties;
            }
        }
        private static AppTexts texts;
        public static AppTexts Texts
        {
            get
            {
                if (texts == null) Load();
                return texts;
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
            //Load settings :
            string settingsPath = Path.GetDirectoryName(ExecutablePath) + "\\" + settingsFileName;
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
                Save(true, false);
            }
            //Load strings :
            string textsPath = Path.GetDirectoryName(ExecutablePath) + "\\" + textsFileName;
            if (File.Exists(textsPath))
            {
                string json = File.ReadAllText(textsPath);
                texts = JsonSerializer.Deserialize<AppTexts>(json);
            }
            else
            {
                //Set default values :
                texts = new AppTexts()
                {
                    language = "EN",
                    strings = new Dictionary<string, Dictionary<string, string>> {
                              {"EN", new Dictionary<string, string>{
                                  {AppTexts.COMMAND_F,"-f <file path> : Transform file"},
                                  {AppTexts.COMMAND_D,"-d <file path or string> <password> : Decrypt data with user password"},
                                  {AppTexts.COMMAND_T,"-t <string> : Transform text"},
                                  {AppTexts.COMMAND_H,"-h : Show available commands"},
                                  {AppTexts.PRESS_ENTER_TO_EXIT,"Press ENTER to exit..."},
                                  {AppTexts.PRESS_ENTER_TO_CONTINUE,"Press ENTER to continue..."},
                                  {AppTexts.OPTIONS,"Options :"},
                                  {AppTexts.TRANSFORM_FILE,"Transform file"},
                                  {AppTexts.TRANSFORM_STRING,"Transform string"},
                                  {AppTexts.DECRYPT_WITH_PASS,"Decrypt data with password"},
                                  {AppTexts.SETTINGS,"Settings"},
                                  {AppTexts.EXIT,"Exit"},
                                  {AppTexts.SELECT_OPTION,"Select an option:"},
                                  {AppTexts.ENABLE,"Enable"},
                                  {AppTexts.DISABLE,"Disable"},
                                  {AppTexts.CONTEXT_MENU,"context menu"},
                                  {AppTexts.DONT_REPLACE,"Don't replace"},
                                  {AppTexts.REPLACE,"Replace"},
                                  {AppTexts.FILE_AFTER_TRANSFORMATION,"original file after transformation"},
                                  {AppTexts.DISSOCIATE,"Dissociate"},
                                  {AppTexts.ASSSOCIATE,"Associate"},
                                  {AppTexts.FILE_EXTENSION,"file extension"},
                                  {AppTexts.BACK,"Back"},
                                  {AppTexts.NOT_AUTHORIZED,"User not authorized"},
                                  {AppTexts.INITIALIZING,"Initializing SmartLock instance"},
                                  {AppTexts.INITIALIZING_ERROR,"Error initializing credentials"},
                                  {AppTexts.WINDOWS_HELLO_NOT_SUPPORTED,"Windows hello not supported or not enabled"},
                                  {AppTexts.INPUT_NEW_PASSWORD,"Input a password (remember it, it is needed as a backup decryption way) :"},
                                  {AppTexts.INPUT_PASSWORD_AGAIN,"One more time :"},
                                  {AppTexts.INPUT_FILE_OR_STRING,"Input a file path or string :"},
                                  {AppTexts.INPUT_PASSWORD,"Input your password :"},
                                  {AppTexts.INPUT_STRING,"Input a string :"},
                                  {AppTexts.INPUT_FILE,"Input file path :"},
                                  {AppTexts.FILE_NOT_EXIST,"File doesn't exist"},
                                  {AppTexts.ENCRYPTED_STRING,"Encrypted string :"},
                                  {AppTexts.DECRYPTED_STRING,"Decrypted string :"},
                                  {AppTexts.START_OF_STRING,"START >"},
                                  {AppTexts.END_OF_STRING,"< END"},
                                  {AppTexts.FILE_DECRYPTED,"File decrypted..."}
                              }
                             }
                    }
                };
                Save(false,true);
            }
        }
        public static void Save(bool saveSettings = true, bool saveTexts = true)
        {
            if (saveSettings)
            {
                string settingsPath = Path.GetDirectoryName(ExecutablePath) + "\\" + settingsFileName;
                try
                {
                    string JSON = JsonSerializer.Serialize(properties);
                    File.WriteAllText(settingsPath, JSON);
                }
                catch (Exception)
                {
                    //Probably write permition is missing
                }
            }
            if (saveTexts)
            {
                string textsPath = Path.GetDirectoryName(ExecutablePath) + "\\" + textsFileName;
                try
                {
                    string JSON = JsonSerializer.Serialize(texts);
                    File.WriteAllText(textsPath, JSON);
                }
                catch (Exception e)
                {
                    //Probably write permition is missing
                }
            }
        }

    }
}