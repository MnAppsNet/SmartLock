using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartLock
{
    class Functions
    {
        static public async Task<SmartLock> InitializeSmartLock(bool statusLogging, SmartLock.StatusChangeHandler statusChangeHandler)
        {//Initialize smartLock instance
            try
            {
                Console.WriteLine(AppSettings.Texts.Get(AppTexts.INITIALIZING));
                SmartLock smartLock = new SmartLock();
                if (statusLogging)
                    smartLock.StatusChange += statusChangeHandler;
                string status = await smartLock.SetupUser();
                while (status != Status.READY)
                {
                    switch (status)
                    {
                        case Status.CRED_ERROR:
                            ShowException(AppSettings.Texts.Get(AppTexts.INITIALIZING_ERROR), "INITIALIZATION");
                            return null;
                        case Status.NO_SUPPORT:
                            ShowException(AppSettings.Texts.Get(AppTexts.WINDOWS_HELLO_NOT_SUPPORTED), "INITIALIZATION");
                            return null;
                        case Status.INPUT_PASSWORD:
                            string userInput; string userInput2;
                            do
                            {
                                Console.WriteLine(AppSettings.Texts.Get(AppTexts.INPUT_NEW_PASSWORD));
                                Console.ForegroundColor = Console.BackgroundColor;
                                userInput = Console.ReadLine();
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine(AppSettings.Texts.Get(AppTexts.INPUT_PASSWORD_AGAIN));
                                Console.ForegroundColor = Console.BackgroundColor;
                                userInput2 = Console.ReadLine();
                                Console.ForegroundColor = ConsoleColor.White;
                            } while (userInput != userInput2);
                            status = await smartLock.SetupUser(userInput);
                            break;
                        case Status.INITIALIZING:
                            continue;
                        case Status.NOT_READY:
                            continue;
                        default:
                            return null;
                    }
                    status = smartLock.GetCurrentStatus();
                }
                return smartLock;
            }catch(Exception e)
            {
                ShowException(e,"INITIALIZE_SMARTLOCK");
                return null;
            }
        }
        static public void DecryptWithPassword(string data = "", string password = "", bool logging = true)
        {
            try
            {
                SmartLock smartLock = new SmartLock();
                Console.Clear();
                if (data == "")
                {
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.INPUT_FILE_OR_STRING));
                    data = Console.ReadLine();
                }
                if (password == "")
                {
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.INPUT_PASSWORD));
                    Console.ForegroundColor = Console.BackgroundColor;
                    password = Console.ReadLine();
                    Console.ForegroundColor = ConsoleColor.White;
                }
                if (data.Trim() == "") return;
                if (File.Exists(data))
                {
                    byte[] fileBytes = File.ReadAllBytes(data);
                    if (smartLock.DecryptWithPassword(ref fileBytes, password) == Status.DECRYPTED)
                    {
                        string filePath = GetDecryptedFilePath(data);
                        File.WriteAllBytes(filePath, fileBytes);
                        if (AppSettings.Properties.ReplaceOriginalFile)
                            File.Delete(data);
                        if (logging)
                        {
                            Console.WriteLine(AppSettings.Texts.Get(AppTexts.FILE_DECRYPTED));
                        }
                    }
                }
                else
                {
                    if (smartLock.DecryptWithPassword(ref data, password) == Status.DECRYPTED)
                    {
                        if (logging)
                        {
                            ShowDecryptedString(data);
                        }
                        else
                        {
                            Console.WriteLine(data);
                        }
                    }
                }
                if (logging)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_CONTINUE));
                    Console.ReadLine();
                }
            }catch(Exception e)
            {
                ShowException(e,"DECRYPT_WITH_PASSWORD");
            }
        }
        static public void TransformString(SmartLock smartLock, string str = "", bool logging = true)
        {//Encrypt/Descrypt string using the given smartLock instance
            try
            {
                Console.Clear();
                if (smartLock == null) {
                    ShowException(AppSettings.Texts.Get(AppTexts.NOT_AUTHORIZED));
                    return; 
                }
                string input = str;
                //bool transformed = false;
                if (input == "")
                {
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.INPUT_STRING));
                    input = Console.ReadLine(); //Get file path
                }
                if (smartLock.DataEncrypted(input))
                {
                    if (smartLock.DecryptString(ref input) == Status.DECRYPTED)
                    {
                        if (logging)
                            ShowDecryptedString(input);
                        else
                            Console.WriteLine(input);

                        //transformed = true;
                    }
                }
                else
                {
                    if (smartLock.EncryptString(ref input) == Status.ENCRYPTED)
                    {
                        if (logging)
                            ShowEncryptedString(input);
                        else
                            Console.WriteLine(input);
                        //transformed = true;
                    }
                }
                ////Copy transformed string to clipboard :
                //if (transformed)
                //{
                //    System.Windows.Forms.Clipboard.SetContent(input);
                //    Console.ForegroundColor = ConsoleColor.Cyan;
                //    Console.WriteLine("\tString copied in clipboard");
                //}
                if (logging)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_CONTINUE));
                    Console.ReadLine();
                }
            }catch(Exception e)
            {
                ShowException(e,"TRANSFORM_STRING");
            }
        }
        static public void TransformFile(SmartLock smartLock, string filePath = "", bool logging = true)
        {//Encrypt/Descrypt file using the given smartLock instance
            try
            {
                Console.Clear();
                if (smartLock == null)
                {
                    ShowException(AppSettings.Texts.Get(AppTexts.NOT_AUTHORIZED));
                    return;
                }
                string file = filePath;
                if (file != "")
                {
                    if (!File.Exists(file))
                    {
                        file = "";
                    }
                }
                if (file == "")
                {
                    Console.Write(AppSettings.Texts.Get(AppTexts.INPUT_FILE));
                    file = Console.ReadLine(); //Get file path
                }
                if (!File.Exists(file))
                {
                    ShowException(AppSettings.Texts.Get(AppTexts.FILE_NOT_EXIST), "TRANSFORM_FILE");
                    return;
                }
                byte[] fileBytes = File.ReadAllBytes(file); //Read file bytes in memory
                if (smartLock.DataEncrypted(fileBytes))
                {
                    if (smartLock.Decrypt(ref fileBytes) == Status.DECRYPTED)
                    {
                        string path = GetDecryptedFilePath(file);
                        File.WriteAllBytes(path, fileBytes);
                        if (AppSettings.Properties.ReplaceOriginalFile && file != path)
                        {
                            File.Delete(file);
                        }
                    }
                }
                else
                {
                    if (smartLock.Encrypt(ref fileBytes) == Status.ENCRYPTED)
                    {
                        string path = GetEncryptedFilePath(file);
                        File.WriteAllBytes(path, fileBytes);
                        if (AppSettings.Properties.ReplaceOriginalFile && file != path)
                        {
                            File.Delete(file);
                        }
                    }
                }
                if (logging)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_CONTINUE));
                    Console.ReadLine();
                }
            }catch(Exception e)
            {
                ShowException(e, "TRANSFORM_FILE");
            }
        }
        static public bool ContectMenuActionActive()
        {
            const string smartLockKey = "SmartLock";
            try
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"*\shell\" + smartLockKey, false);
                if (key == null)
                    return false;
                return true;
            }catch(Exception e)
            {
                ShowException(e, "CONTEXT_MENU_ACTION_ACTIVE");
                return false;
            }
        }
        static public bool HandleContextMenuAction()
        {//Enable/Disable windows context menu action for smartlock
            const string smartLockKey = "SmartLock";
            const string actionText = "Transform file";
            string path = AppSettings.ExecutablePath;
            try
            {
                RegistryKey key = null;
                if (!ContectMenuActionActive())
                {//Context Menu Action doesn't exist, create it
                    key = Registry.ClassesRoot.OpenSubKey(@"*\shell", true);
                    key.CreateSubKey(smartLockKey);
                    if (key == null)
                    {
                        //Failed, probably authorization needed
                        return false;
                    }
                    key = Registry.ClassesRoot.OpenSubKey(@"*\shell\" + smartLockKey, true);
                    key.SetValue("", actionText);
                    key.SetValue("Icon", path);
                    key = key.CreateSubKey("command");
                    if (key == null)
                    {
                        //Failed, probably authorization needed
                        return false;
                    }
                    key.SetValue("", "\"" + path + "\" \"%L\"");
                }
                else
                {
                    key = Registry.ClassesRoot.OpenSubKey(@"*\shell", true);
                    if (key == null)
                        return false;
                    key.DeleteSubKeyTree(smartLockKey);
                }
                if (key != null) key.Close();
                return true;
            }
            catch (Exception e)
            {
                //Failed, probably authorization needed
                ShowException(e,"HANDLE_CONTEXT_MENU_ACTION");
                return false;
            }
        }
        static public bool FileExtensionAssociated()
        {
            try
            {
                RegistryKey key = null;
                key = Registry.ClassesRoot.OpenSubKey(AppSettings.RegistryAssiciationEntry, false);
                if (key == null)
                    return false;
                else
                    return true;
            }
            catch (Exception e)
            {
                ShowException(e, "FILE_EXTENSION_ASSOCIATED");
                return false;
            }
        }
        static public bool HandleFileExtensionAssociation()
        {
            try {
                string path = AppSettings.ExecutablePath;
                string iconPath = AppSettings.IconPath;
                RegistryKey key = null;
                if (!FileExtensionAssociated())
                {//Enable extention assiciation

                    //Set SmartLock action on registry :
                    key = Registry.ClassesRoot.CreateSubKey(AppSettings.RegistryAssiciationEntry);
                    if (key == null) return false;
                    key = key.CreateSubKey("DefaultIcon");
                    if (key == null) return false;
                    if (!File.Exists(iconPath))
                    {
                        File.WriteAllBytes(iconPath,Properties.Resources.fileIcon);
                    }
                    key.SetValue("", iconPath);
                    key = Registry.ClassesRoot.OpenSubKey(AppSettings.RegistryAssiciationEntry,true);
                    key = key.CreateSubKey("shell");
                    if (key == null) return false;
                    key = key.CreateSubKey("Open");
                    if (key == null) return false;
                    key = key.CreateSubKey("Command");
                    if (key == null) return false;
                    key.SetValue("", "\"" + path + "\" \"%L\"");

                    //Associate SmartLock action with extension :
                    key = Registry.ClassesRoot.OpenSubKey(AppSettings.EncryptedFileExtention, true);
                    if (key == null)
                    {
                        key = Registry.ClassesRoot.CreateSubKey(AppSettings.EncryptedFileExtention);
                        if (key == null) return false;
                        key.SetValue("", AppSettings.RegistryAssiciationEntry);
                    }
                    else key.SetValue("", AppSettings.RegistryAssiciationEntry);
                }
                else
                {//Disable extention assiciation

                    //Delete SmartLock action from registry :
                    Registry.ClassesRoot.DeleteSubKeyTree(AppSettings.RegistryAssiciationEntry);

                    //Dissociation SmartLock action with extension :
                    key = Registry.ClassesRoot.OpenSubKey(AppSettings.EncryptedFileExtention, true);
                    if (key != null)
                    {
                        key.SetValue("", "");
                    }
                }
                if (key != null) key.Close();
                return true;
            }
            catch (Exception e)
            {
                ShowException(e, "HANDLE_FILE_EXTENSION_ASSOCIATION");
                return false;
            }
        }
        static private void ShowException(Exception e, string messageGroup = "")
        {
            Console.Clear();
            Console.WriteLine((messageGroup != "")?(messageGroup + ": " + e.Message):e.Message);
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_CONTINUE));
            Console.ReadLine();
        }
        static private void ShowException(string exception, string messageGroup = "")
        {
            Console.Clear();
            Console.WriteLine((messageGroup != "") ? (messageGroup + ": " + exception) : exception);
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_CONTINUE));
            Console.ReadLine();
        }
        static private void ShowEncryptedString(string str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.ENCRYPTED_STRING));
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.START_OF_STRING));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.END_OF_STRING));
        }
        static private void ShowDecryptedString(string str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.DECRYPTED_STRING));
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.START_OF_STRING));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.END_OF_STRING));
        }
        static private string GetDecryptedFilePath(string file)
        {
            string path = file;
            if (path.EndsWith(AppSettings.EncryptedFileExtention))
                path = path.Substring(0, path.Length - AppSettings.EncryptedFileExtention.Length);
            if (!AppSettings.Properties.ReplaceOriginalFile)
            {
                while (File.Exists(path))
                {
                    string ext = Path.GetExtension(path);
                    if (ext != "") //<= Path has a file extention
                        path = path.Replace(ext, AppSettings.EncryptedFileExtention + ext);
                    else//<= No file extention
                        path = path + AppSettings.EncryptedFileExtention.Replace(".", "");
                }
            }
            return path;
        }
        static private string GetEncryptedFilePath(string file)
        {
            string path = file;
            path += AppSettings.EncryptedFileExtention;
            if (!AppSettings.Properties.ReplaceOriginalFile)
            {
                while (File.Exists(path))
                {
                    path += AppSettings.EncryptedFileExtention;
                }
            }
            return path;
        }
    }
}