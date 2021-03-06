using System;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using System.Security.Cryptography;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;
using System.Diagnostics;

namespace SmartLock
{
    class SmartLock
    {
        //External native windows functions (external unmanaged dynamic-link library [DLL]) - Start >
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        //[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        //[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        //static extern bool GetCursorPos(out POINT lpPoint);
        //[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        //public struct POINT
        //{
        //    public int X;
        //    public int Y;

        //    public static implicit operator System.Drawing.Point(POINT point)
        //    {
        //        return new System.Drawing.Point(point.X, point.Y);
        //    }
        //}
        //External native windows functions (external unmanaged dynamic-link library [DLL]) - < End
        
        //Private Constrants :
        private const string SALT = @"nF#(f)J<C3494c,m$<)@!~r,@+\##9r<$ fjdlfsjmgs4lsefc.,a<"; //SALT applied on password to generate secret key for AES encryption
        private const string VECTOR = @"i#9di2)3%Fd:ewfR";  //Initialization vector used with AES encryption, 16-byte
        private const string ENCRYPTED_IDENTIFIER = @"SmartLock_Encrypted_Data_osfj93fjso93fj39rw3"; //A string used to generate the indentifier used to understand if data are encrypted

        //Private Variables :
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1); //Windows TOPMOST indicator
        private const UInt32 SWP_NOSIZE = 0x0001;                     //Don't change window size indicator
        private const UInt32 SWP_NOSENDCHANGING = 0x0400;             //Don't allow other window changes indicator
        private const UInt32 SWP_SHOWWINDOW = 0x0040;                 //Show windpow indicator
        private const UInt32 SWP_NOMOVE = 0x0002;                     //Don't change window possition indicator
        private const UInt32 TOPMOST_FLAGS = SWP_SHOWWINDOW | SWP_NOSIZE | SWP_NOSENDCHANGING | SWP_NOMOVE; //Flags above combined

        private string status;                      //Set to true if Security instance is ready to be used
        private string encryptionKey;               //encryptionKey encrypted with the instanceKey
        private string encryptedIdentifier;         //An identifier used to understand if a file is encrypted with SmartLock
        private readonly SHA256 hash;               //SHA256 hash instance
        private readonly Aes symmetricEncription;   //AES symmetric encryption instance    
        private readonly string instanceKey;        //Randomly generated key used to encrypt important data in memory

        //Events :
        public delegate void StatusChangeHandler(string status);
        public event StatusChangeHandler StatusChange;

        //***************
        // Constructors *
        //******************************************************************************************************************************************
        public SmartLock(string userPassword = "")
        {
            //Generate a random instance key :
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(Guid.NewGuid().ToByteArray());
                ms.Write(Encoding.UTF8.GetBytes(DateTime.Now.ToLongDateString()));
                instanceKey = Convert.ToBase64String(ms.ToArray());
            }
            hash = SHA256.Create();                 //Create SHA256 hash instance
            symmetricEncription = Aes.Create();     //Create AES symmetric encryption instance
            encryptionKey = null;
        }

        //*****************
        // Public Section *
        //******************************************************************************************************************************************
        public string GetCurrentStatus()
        {
            return status; //Return current status of SmartLock instance
        }
        public string GetCurrentStatusDescription()
        {
            return Status.Get(status); //Return current status description of SmartLock instance
        }
        public async Task<string> SetupUser(string userPassword = "")
        {
            //Local viriables :
            KeyCredentialRetrievalResult KeyCredential;                     //Credential results of Windows Hello (key pair managed by Windows Hello libraries)
            ChangeStatus(Status.INITIALIZING);
            //Check Windows Hello Availability :
            if (!await WindowsHelloAvailable())
            {
                ChangeStatus(Status.NO_SUPPORT);
                return status;
            }
            if (SettingsHandler.Settings.userID == null)
            {
                if (userPassword == "")
                {
                    //If a userID doesn't exists, no ecryption key is configured yet
                    //Ask for a user password to generate a new encryption key
                    ChangeStatus(Status.INPUT_PASSWORD);
                    return status;
                }
                //If no userID exist and a password is given, create a new credential in Windows Hello,
                //generate an encryption key and store necessary values
                SettingsHandler.Settings.userID = GetUserID(userPassword);

                KeyCredential = await ShowWindowsHelloPopup(true);
            }
            else
            {
                //If a UserID exists, Windows Hello credentials have already been generated,
                //request them and get the encryption key
                KeyCredential = await ShowWindowsHelloPopup(false);

            }
            if (KeyCredential.Status != KeyCredentialStatus.Success)
            {
                ChangeStatus(Status.CRED_ERROR);
                return status; //An error occurred during Windows Hello credential retrieval
            }

            //Generate encryption key :
            string key = await SignString(KeyCredential.Credential, SettingsHandler.Settings.userID);
            if (key == null)
            {
                ChangeStatus(Status.NOT_AUTHORIZED);
                return status; //An error occured during the generation of the encryption key
            }
            //Validate encryption key :
            if ((SettingsHandler.Settings.key == "" || SettingsHandler.Settings.key == null) && userPassword != "") //<= Initial run
            {
                //In case of initial execution, gather the needed key values :
                SettingsHandler.Settings.key = EncryptString(key, userPassword);                                          //Save the key encrypted with the user password
                SettingsHandler.Settings.keyHash = GetFingerprint(key); //Save the hash of the key
                SettingsHandler.Save();
            }
            else //<= Check if key generated is valid before file encryption
            {
                //Check if the saved key hash is the same as the at this run :
                string keyHash = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(key)));
                if (keyHash != GetFingerprint(key))
                {
                    //If the hashes are different, something is not going right, abort file encryption/decryption to avoid data corruption
                    ChangeStatus(Status.INVALID_KEY);
                    return status;
                }
            }
            encryptionKey = EncryptString(key, instanceKey);
            ChangeStatus(Status.READY);
            return status; //Security instance is ready to encrypt/decrypt files
        }
        public string EncryptString(ref string str)
        {
            string results;
            try
            {
                byte[] strBytes = Encoding.UTF8.GetBytes(str);
                results = Encrypt(ref strBytes);
                if (results == Status.ENCRYPTED)
                {
                    str = Convert.ToBase64String(strBytes);
                }
            }
            catch (Exception)
            {
                ChangeStatus(Status.ENC_FAIL);
                results = status;
            }
            return results;
        }
        public string DecryptString(ref string str, string encKey = "")
        {
            string results;
            try
            {
                byte[] strBytes = Convert.FromBase64String(str);
                results = Decrypt(ref strBytes, encKey);
                if (results == Status.DECRYPTED)
                {
                    str = Encoding.UTF8.GetString(strBytes);
                }
            }
            catch (Exception)
            {
                ChangeStatus(Status.DEC_FAIL);
                results = status;
            }
            return results;
        }
        public string Encrypt(ref byte[] data)
        {
            if (encryptionKey == null)
            {
                ChangeStatus(Status.NOT_READY);
                return status;
            }
            byte[] transformedData = new byte[data.Length];
            byte[] vectorBytes = Encoding.UTF8.GetBytes(VECTOR);
            byte[] saltBytes = Encoding.UTF8.GetBytes(SALT);
            byte[] encIdentifier = Encoding.UTF8.GetBytes(GetEncryptedIdentifier());
            const int keySize = 256;
            ChangeStatus(Status.ENCRYPTING);
            try
            {
                using AesManaged cipher = new AesManaged();
                PasswordDeriveBytes passBytes = new PasswordDeriveBytes(
                    Encoding.UTF8.GetBytes(
                        DecryptString(encryptionKey, instanceKey)
                        ),
                    saltBytes
                );
                byte[] keyBytes = passBytes.GetBytes(keySize / 8);
                cipher.Mode = CipherMode.CBC;
                using ICryptoTransform encryptor = symmetricEncription.CreateEncryptor(keyBytes, vectorBytes);
                using MemoryStream to = new MemoryStream();
                using CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write);
                writer.Write(data, 0, data.Length);
                writer.FlushFinalBlock();
                transformedData = to.ToArray();
            }
            catch (Exception e)
            {
                ChangeStatus(Status.ENC_FAIL);
                throw e;
            }
            //Gather encrypted data :
            data = new byte[transformedData.Length + encIdentifier.Length];
            Buffer.BlockCopy(encIdentifier, 0, data, 0, encIdentifier.Length);
            Buffer.BlockCopy(transformedData, 0, data, encIdentifier.Length, transformedData.Length);
            ChangeStatus(Status.ENCRYPTED);
            return status;
        }
        public string Decrypt(ref byte[] data, string encKey = "")
        {
            string key = encryptionKey;
            if (encKey != "") 
                key = encKey; //<= If a key is provided use this instead
            else
                key = DecryptString(key, instanceKey);
            if (key == null)
            {
                ChangeStatus(Status.NOT_READY);
                StatusChange.Invoke(status);
                return status; //Instance not ready yet
            }
            byte[] vectorBytes = Encoding.UTF8.GetBytes(VECTOR);
            byte[] saltBytes = Encoding.UTF8.GetBytes(SALT);
            byte[] encIdentifier = Encoding.UTF8.GetBytes(GetEncryptedIdentifier());
            byte[] transformedData = new byte[data.Length - encIdentifier.Length];
            if (!DataEncrypted(data))
            {
                ChangeStatus(Status.NOT_ENC);
                return status; //Encryption identifier is wrong / Data not encrypted with SmartLock
            }
            //Remove encryption identifier :
            Buffer.BlockCopy(data, encIdentifier.Length, transformedData, 0, data.Length - encIdentifier.Length);
            const int keySize = 256;
            ChangeStatus(Status.DECRYPTING);
            try
            {
                using AesManaged cipher = new AesManaged();
                PasswordDeriveBytes passBytes = new PasswordDeriveBytes(
                        Encoding.UTF8.GetBytes(
                            key
                            ),
                        saltBytes
                );
                byte[] keyBytes = passBytes.GetBytes(keySize / 8);
                cipher.Mode = CipherMode.CBC;
                using ICryptoTransform decryptor = symmetricEncription.CreateDecryptor(keyBytes, vectorBytes);
                using MemoryStream to = new MemoryStream(transformedData);
                using CryptoStream reader = new CryptoStream(to, decryptor, CryptoStreamMode.Read);
                transformedData = new byte[data.Length - encIdentifier.Length];
                reader.Read(transformedData, 0, transformedData.Length);
                //transformedData = to.ToArray();
            }
            catch (Exception e)
            {
                ChangeStatus(Status.DEC_FAIL);
                throw e;
            }

            data = transformedData; //<= Pass the transformed data back
            ChangeStatus(Status.DECRYPTED);
            return status;
        }
        public string DecryptWithPassword(ref string input, string password)
        {
            if (SettingsHandler.Settings.key == null || SettingsHandler.Settings.key == "")
            {
                ChangeStatus(Status.NO_KEY);
                return status; //<= No encryption key is configured
            }
            string encKey;
            try
            {
                encKey = DecryptString(SettingsHandler.Settings.key, password);
                string f = GetFingerprint(encKey);
                if (f != SettingsHandler.Settings.keyHash)
                {
                    ChangeStatus(Status.WRONG_KEY);
                    return status;
                }
            }
            catch (Exception)
            {
                ChangeStatus(Status.WRONG_KEY);
                return status;
            }
            return DecryptString(ref input,encKey);
        }
        public string DecryptWithPassword(ref byte[] input, string password)
        {
            try
            {
                if (SettingsHandler.Settings.key == null || SettingsHandler.Settings.key == "")
                {
                    ChangeStatus(Status.NO_KEY);
                    return status; //<= No encryption key is configured
                }
                string encKey = DecryptString(SettingsHandler.Settings.key, password);
                if (GetFingerprint(encKey) != SettingsHandler.Settings.keyHash)
                {
                    ChangeStatus(Status.WRONG_KEY);
                    return status;
                }
                return Decrypt(ref input, encKey);
            }
            catch (Exception)
            {
                ChangeStatus(Status.DEC_FAIL);
                return status;
            }
        }
        public bool DataEncrypted(byte[] data)
        {
            //Get encrypted identifier bytes :
            byte[] encIdentifier = Encoding.UTF8.GetBytes(GetEncryptedIdentifier());
            //Check if encryption identifier is correct :
            byte[] identifier = new byte[encIdentifier.Length];
            if (data.Length < encIdentifier.Length)
                return false; //<= Encrypted minimun size is the size of the encryption identifier
            Buffer.BlockCopy(data, 0, identifier, 0, encIdentifier.Length);
            string identifierStr = Encoding.UTF8.GetString(identifier);
            if (identifierStr == GetEncryptedIdentifier())
                return true;
            return false;
        }
        public bool DataEncrypted(string data) //DATA is a BASE64 encoded string
        {
            try
            {
                //Get encrypted identifier bytes :
                byte[] encIdentifier = Encoding.UTF8.GetBytes(GetEncryptedIdentifier());
                //Check if encryption identifier is correct :
                byte[] identifier = new byte[encIdentifier.Length];
                byte[] dataBytes = Convert.FromBase64String(data);
                if (dataBytes.Length < encIdentifier.Length)
                    return false; //<= Encrypted minimun size is the size of the encryption identifier
                Buffer.BlockCopy(dataBytes, 0, identifier, 0, encIdentifier.Length);
                string identifierStr = Encoding.UTF8.GetString(identifier);
                if (identifierStr == GetEncryptedIdentifier())
                    return true;
                return false;
            }
            catch (Exception)
            {
                return false; //<= BASE64 convertion error, string probably not encrypted if not a BASE64 format
            }
        }
        public void SetLanguage(string lang)
        {//Set SmartLock output message language
            if (!SettingsHandler.Settings.status.ContainsKey(lang))
            {
                ChangeStatus(Status.LANG_NOT_VALID);
                return;
            }
            SettingsHandler.Settings.language = lang;
            SettingsHandler.Save();
        }
        //******************
        // Private Section *
        //******************************************************************************************************************************************
        private async Task<KeyCredentialRetrievalResult> ShowWindowsHelloPopup(bool newUser)
        {
            const string windowsHelloAuthorizationProcess = "CredentialUIBroker.exe";
            KeyCredentialCreationOption optionNew = KeyCredentialCreationOption.ReplaceExisting;
            Windows.Foundation.IAsyncOperation<KeyCredentialRetrievalResult> windowsHelloRequestTask;
            if (newUser)
                windowsHelloRequestTask = KeyCredentialManager.RequestCreateAsync(SettingsHandler.Settings.userID, optionNew);
            else
                windowsHelloRequestTask = KeyCredentialManager.OpenAsync(SettingsHandler.Settings.userID);
            Task bringWindowsHelloPopupToFront = new Task(() =>
            {
                Process[] processes;
                //POINT cursor;
                int i = 20; //Iterate for ~10 seconds until the window is found (20 iteration every 500ms)
                Task.Delay(2000); //Wait 2 seconds before start searching
                do
                {
                    Task.Delay(500);//Wait 0.5 second
                    processes = Process.GetProcesses();
                    Process process = Array.Find<Process>(processes, (p) =>
                    {
                        try
                        {
                            return p.MainModule.ModuleName == windowsHelloAuthorizationProcess;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    });
                    if (process != null)
                    {
                        //GetCursorPos(out cursor);
                        int x = 0; // cursor.X;
                        int y = 0; // cursor.Y;
                        SetWindowPos(process.MainWindowHandle, HWND_TOPMOST, x, y, 0, 0, TOPMOST_FLAGS);
                        i = -1; //Exit loop
                    }
                    i--;
                } while (i >= 0);
            });
            Task<KeyCredentialRetrievalResult> requestTast = Task.Run(async () => { return await windowsHelloRequestTask; });
            bringWindowsHelloPopupToFront.Start();
            return await requestTast;
        }
        private async Task<bool> WindowsHelloAvailable()
        {//Checks if windows hello is supported and used by the user
            bool keyCredentialAvailable = await KeyCredentialManager.IsSupportedAsync();
            if (!keyCredentialAvailable)
            {
                return false; // User didn't set up PIN yet or Windows Hello not supported
            }
            return true;
        }
        private string GetFingerprint(string input)
        {//Generates a unique finger print of the given input string. This is a one-way function.
            return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(input)));
            // (INPUT) => (Input FingerPrint) \\
        }
        private string GetUserID(string password)
        {//Generate UserID from user password
            return GetFingerprint(password).Replace("/","").Replace("\\","");
        }
        private async Task<string> SignString(KeyCredential credential, string input)
        {//Sign string with private key
            var buffer = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8 );
            var results = await credential.RequestSignAsync(buffer);
            if (results.Status != KeyCredentialStatus.Success)
            {
                return null; //An error occured during the sign of the key
            }
            return Convert.ToBase64String(results.Result.ToArray()); //return BASE64 of signed input
        }
        private string EncryptString(string input, string pass) //input = string to be encrypted
        {
            byte[] vectorBytes = Encoding.UTF8.GetBytes(VECTOR.Substring(0, 16)); //16-byte
            byte[] saltBytes = Encoding.UTF8.GetBytes(SALT);
            byte[] inputBytes = Encoding.UTF8.GetBytes(input.Trim('\0'));
            byte[] encrypted;
            const int keySize = 256;

            using (AesManaged cipher = new AesManaged())
            {
                PasswordDeriveBytes passBytes =
                    new PasswordDeriveBytes(pass, saltBytes);
                byte[] keyBytes = passBytes.GetBytes(keySize / 8);
                cipher.Mode = CipherMode.CBC;
                using ICryptoTransform encryptor = symmetricEncription.CreateEncryptor(keyBytes, vectorBytes);
                using MemoryStream to = new MemoryStream();
                using CryptoStream writer = new CryptoStream(to, encryptor, CryptoStreamMode.Write);
                writer.Write(inputBytes, 0, inputBytes.Length);
                writer.FlushFinalBlock();
                encrypted = to.ToArray();
            }
            return Convert.ToBase64String(encrypted);
        }
        private string DecryptString(string input, string pass) //input = encrypted data in base64 format
        {
            byte[] vectorBytes = Encoding.UTF8.GetBytes(VECTOR.Substring(0, 16)); //16-byte
            byte[] saltBytes = Encoding.UTF8.GetBytes(SALT);
            byte[] inputBytes = Convert.FromBase64String(input);
            byte[] decrypted;
            const int keySize = 256;

            using (AesManaged cipher = new AesManaged())
            {
                PasswordDeriveBytes passBytes =
                    new PasswordDeriveBytes(pass, saltBytes);
                byte[] keyBytes = passBytes.GetBytes(keySize / 8);
                cipher.Mode = CipherMode.CBC;
                using ICryptoTransform decryptor = symmetricEncription.CreateDecryptor(keyBytes, vectorBytes);
                using MemoryStream to = new MemoryStream(inputBytes);
                using CryptoStream reader = new CryptoStream(to, decryptor, CryptoStreamMode.Read);
                decrypted = new byte[inputBytes.Length];
                reader.Read(decrypted, 0, decrypted.Length);
            }
            return Encoding.UTF8.GetString(decrypted).Trim('\0');
        }
        private void ChangeStatus(string stat)
        {
            status = stat;
            if (StatusChange != null)
                StatusChange.Invoke(status);
        }
        private string GetEncryptedIdentifier()
        {//Returns an identifier that is used to understand if a file is encrypted with SmartLock
            if(encryptedIdentifier == null || encryptedIdentifier == "") 
                encryptedIdentifier = GetFingerprint(ENCRYPTED_IDENTIFIER); //< Lazy population, populated only when first needed and kept for later use
            return encryptedIdentifier;
        }
    }
}
