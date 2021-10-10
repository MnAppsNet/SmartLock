using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SmartLock
{
    class Status
    {
        public static string language = "EN";

        public const string READY = "READY";                    //Program is ready to proceed
        public const string CRED_ERROR = "CRED_ERROR";          //An error occured durring credential retrieval
        public const string INPUT_PASSWORD = "INPUT_PASSWORD";  //A user password is required
        public const string NO_SUPPORT = "NO_SUPPORT";          //No support of windows hello or not enabled
        public const string NOT_AUTHORIZED = "NOT_AUTHORIZED";  //User not authorized with Windows Hello
        public const string INVALID_KEY = "INVALID_KEY";        //Encryption key is not valid
        public const string NOT_READY = "NOT_READY";            //Instance not ready yet
        public const string INITIALIZING = "INITIALIZING";      //Initializing instance
        public const string ENCRYPTED = "ENCRYPTED";            //File encrypted
        public const string DECRYPTED = "DECRYPTED";            //File decrypted
        public const string ENCRYPTING = "ENCRYPTING";          //File is beeing encrypted
        public const string DECRYPTING = "DECRYPTING";          //File id beeing decrypted
        public const string ENC_FAIL = "ENC_FAIL";              //Encryption failed
        public const string DEC_FAIL = "DEC_FAIL";              //Decryption failed
        public const string NOT_ENC = "NOT_ENC";                //Data are not encrypted with SmartLock
        public const string WRONG_KEY = "WRONG_KEY";            //User password was incorrect
        public const string NO_KEY = "NO_KEY";                  //No encryption key is configured
        public const string LANG_NOT_VALID = "LANG_NOT_VALID";  //Language not valid

        public static string Get(string status)
        {
            try
            {
                return SettingsHandler.Settings.status[language][status];
            }catch(Exception e)
            {
                throw e;
            }
        }
    }
    class Settings
    {
        public string userID { get; set; }    //BASE64 string of the user password hash
        public string key { get; set; }       //Encrypted key, that is used to encrypt files, with user password
        public string keyHash { get; set; }   //BASE64 string of the hash of the key used to encrypt files
        public string language { get; set; }  //SmartLock language
        public Dictionary<string, Dictionary<string, string>> status { get; set; }
    }
    class SettingsHandler
    {
        public static readonly string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public const string settingsFileName = "smartlock.json";

        private static Settings settings;
        public static Settings Settings
        {
            get
            {
                if(settings == null) Load();
                return settings;
            }
        }
        private static void Load()
        {
            string settingsFile = path + "\\" + settingsFileName; //Location of settings file
            try
            {
                if (File.Exists(settingsFile))
                {
                    string JSON = File.ReadAllText(settingsFile);
                    settings = JsonSerializer.Deserialize<Settings>(JSON);
                    if (settings.status.ContainsKey(settings.language))
                        Status.language = settings.language;
                }
                else
                {
                    //Set default values :
                    settings = new Settings
                    {
                        userID = null,
                        key = null,
                        keyHash = null,
                        language = "EN",
                        status = new Dictionary<string, Dictionary<string, string>> {
                            {"EN", new Dictionary<string, string>{
                                {Status.READY,"Application initialized and ready to encrypt/decrypt"},
                                {Status.CRED_ERROR,"An error occured durring credential retrieval"},
                                {Status.INPUT_PASSWORD,"User password is required"},
                                {Status.NO_SUPPORT,"No support of windows hello or not enabled"},
                                {Status.NOT_AUTHORIZED,"User not authorized with Windows Hello"},
                                {Status.INVALID_KEY,"Encryption key is not valid"},
                                {Status.NOT_READY,"Instance not ready yet"},
                                {Status.INITIALIZING,"Initializing application"},
                                {Status.ENCRYPTED,"Data encrypted"},
                                {Status.DECRYPTED,"Data decrypted"},
                                {Status.ENCRYPTING,"Data is beeing encrypted"},
                                {Status.DECRYPTING,"Data id beeing decrypted"},
                                {Status.ENC_FAIL,"Encryption failed"},
                                {Status.DEC_FAIL,"Decryption failed"},
                                {Status.NOT_ENC,"Data are not encrypted with SmartLock"},
                                {Status.WRONG_KEY,"User password was incorrect"},
                                {Status.NO_KEY,"No encryption key is configured"},
                                {Status.LANG_NOT_VALID,"Language not valid"}
                            }
                           }
                          }
                        };
                    Save();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public static void Save()
        {
            try
            {
                string settingsFile = path + "\\" + settingsFileName; //Location of settings file
                string JSON = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsFile, JSON);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
