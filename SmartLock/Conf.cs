using System;
using System.Collections.Generic;
using System.Text;

namespace SmartLock
{
    class Settings
    {
        public string userID { get; set; }    //BASE64 string of the user password hash
        public string key { get; set; }       //Encrypted key, that is used to encrypt files, with user password
        public string keyHash { get; set; }   //BASE64 string of the hash of the key used to encrypt files
        public string language { get; set; }  //SmartLock language
    }
    class Status
    {
        public static string language = "EN";

        public const UInt16 READY = 0;              //Program is ready to proceed
        public const UInt16 CRED_ERROR = 1;         //An error occured durring credential retrieval
        public const UInt16 INPUT_PASSWORD = 2;     //A user password is required
        public const UInt16 NO_SUPPORT = 3;         //No support of windows hello or not enabled
        public const UInt16 NO_SIGN = 4;            //Failed to sign userID
        public const UInt16 INVALID_KEY = 5;        //Encryption key is not valid
        public const UInt16 NOT_READY = 6;          //Instance not ready yet
        public const UInt16 INITIALIZING = 7;       //Initializing instance
        public const UInt16 ENCRYPTED = 9;          //File encrypted
        public const UInt16 DECRYPTED = 10;         //File decrypted
        public const UInt16 ENCRYPTING = 11;        //File is beeing encrypted
        public const UInt16 DECRYPTING = 12;        //File id beeing decrypted
        public const UInt16 ENC_FAIL = 13;          //Encryption failed
        public const UInt16 DEC_FAIL = 14;          //Decryption failed
        public const UInt16 NOT_ENC = 15;           //Data are not encrypted with SmartLock
        public const UInt16 WRONG_KEY = 16;         //User password was incorrect
        public const UInt16 NO_KEY = 17;            //No encryption key is configured

        public static readonly Dictionary<string, Dictionary<UInt16, string>> Descriptions = new Dictionary<string, Dictionary<UInt16, string>> {
          {"EN", new Dictionary<ushort, string>{
              {READY,"Application initialized and ready to encrypt/decrypt"},
              {CRED_ERROR,"An error occured durring credential retrieval"},
              {INPUT_PASSWORD,"User password is required"},
              {NO_SUPPORT,"No support of windows hello or not enabled"},
              {NO_SIGN,"Failed to sign userID"},
              {INVALID_KEY,"Encryption key is not valid"},
              {NOT_READY,"Instance not ready yet"},
              {INITIALIZING,"Initializing application"},
              {ENCRYPTED,"Data encrypted"},
              {DECRYPTED,"Data decrypted"},
              {ENCRYPTING,"Data is beeing encrypted"},
              {DECRYPTING,"Data id beeing decrypted"},
              {ENC_FAIL,"Encryption failed"},
              {DEC_FAIL,"Decryption failed"},
              {NOT_ENC,"Data are not encrypted with SmartLock"},
              {WRONG_KEY,"User password was incorrect"},
              {NO_KEY,"No encryption key is configured"}
          }
         }
        };
    }
}
