using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Windows.Security.Credentials;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace SmartLock
{
    class Commands
    {
        public const string file = "-f";
        public const string fileDescription = "-f <file path> : Transform file";
        public const string text = "-t";
        public const string textDescription = "-t <string> : Transform text";
        public const string decrypt = "-d";
        public const string decrypttDescription = "-d <file path or string> <password> : Decrypt data with user password";
        public const string help = "-h";
        public const string helpDescription = "-h : Show available commands";
    }
    class Program
    {
        static async Task Main(string[] args)
        {//Entry point
            if (args.Length == 0)
            {//Normal execution
                await ShowMenu();
            }
            else
            {//Execution with arguments
                await HandleCommands(args);
            }
        }

        static private async Task ShowMenu()
        {//Show main menu with all the available options
            SmartLock smartLock = await InitializeSmartLock(true);
            bool exit = false;
            do
            {
                UInt16 option = ShowOptions();
                switch (option)
                {
                    case 1://Transform File
                        TransformFile(smartLock);
                        break;
                    case 2://Transform string
                        TransformString(smartLock);
                        break;
                    case 3://Decrypt with user password
                        DecryptWithPassword(smartLock);
                        break;
                    case 4://Show settings
                        //Under construction...
                        break;
                    default://Exit
                        exit = true;
                        break;
                }
            } while (!exit);
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
        private static async Task HandleCommands(string[] args)
        {//Handle execution with arguments

            if (args.Length >= 2)
            {
                SmartLock smartLock = await InitializeSmartLock(false);
                switch (args[0].ToLower())
                {
                    case Commands.file:
                        TransformFile(smartLock, args[1], false);
                        break;
                    case Commands.text:
                        TransformString(smartLock, args[1], false);
                        break;
                    case Commands.decrypt:
                        if (args.Length < 3)
                            ShowHelp();
                        else
                            DecryptWithPassword(smartLock, args[1], args[2], false);
                        break;
                    default:
                        ShowHelp();
                        break;
                }
            }
            else
            {
                if (args[0] == Commands.help)
                    ShowHelp();
                else
                {
                    //By default assume that we are getting a file path, execute file transformation
                    SmartLock smartLock = await InitializeSmartLock(false);
                    TransformFile(smartLock, args[0], false);
                }
            }
        }
        static private async Task<SmartLock> InitializeSmartLock(bool statusLogging)
        {//Initialize smartLock instance
            Console.WriteLine("Initializing SmartLock instance...");
            SmartLock smartLock = new SmartLock();
            if (statusLogging)
                smartLock.StatusChange += StatusChanged;
            UInt16 status = await smartLock.SetupUser();
            while (status != Status.READY)
            {
                switch (status)
                {
                    case Status.CRED_ERROR:
                        Console.WriteLine("Error initializing credentials");
                        return null;
                    case Status.NO_SUPPORT:
                        Console.WriteLine("Windows hello not supported or not enabled");
                        return null;
                    case Status.INPUT_PASSWORD:
                        string userInput; string userInput2;
                        do
                        {
                            Console.WriteLine("Input a password (remember it, it is needed as a backup decryption way) :");
                            Console.ForegroundColor = Console.BackgroundColor;
                            userInput = Console.ReadLine();
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine("One more time :");
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
                status = smartLock.GetStatus();
            }
            return smartLock;
        }
        static private void ShowHelp()
        {//Show help menu with all available commands
            List<FieldInfo> options = typeof(Commands).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                                        .Where(fi => fi.IsLiteral && !fi.IsInitOnly).ToList();
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (FieldInfo fi in options)
            {
                if (fi.Name.EndsWith("Description"))
                {
                    Console.WriteLine(fi.GetRawConstantValue());
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        static private UInt16 ShowOptions()
        {//Show application options and return the one selected by the user
            string validOptions = "1.2.3.4.5.";
            string option;
            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("SmartLock");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("----------------------------------------");
                Console.WriteLine("Options :");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("\t1.Transform file");
                Console.WriteLine("\t2.Transform string");
                Console.WriteLine("\t3.Decrypt data with password");
                Console.WriteLine("\t4.Settings");
                Console.WriteLine("\t5.Exit");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("Select an option: ");
                string userInput = Console.ReadLine();
                if (userInput.Length > 0)
                    option = userInput.Substring(0, 1);
                else
                    option = "0";
            } while (!(validOptions.Contains(option + ".")));

            return UInt16.Parse(option);
        }
        static private void DecryptWithPassword(SmartLock smartLock, string data = "", string password = "", bool logging = true)
        {
            if (smartLock == null) return;
            Console.Clear();
            if (data == "")
            {
                Console.WriteLine("Input a file path or string :");
                data = Console.ReadLine();
            }
            if (password == "")
            {
                Console.WriteLine("Input your password :");
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
                    File.WriteAllBytes(data, fileBytes);
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
                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
            }
        }
        static private void TransformString(SmartLock smartLock, string str = "", bool logging = true)
        {//Encrypt/Descrypt string using the given smartLock instance
            if (smartLock == null) return;
            Console.Clear();
            string input = str;
            //bool transformed = false;
            if(input == "")
            {
                Console.WriteLine("Input a string : ");
                input = Console.ReadLine(); //Get file path
            }
            if (smartLock.DataEncrypted(input))
            {
                if (smartLock.DecryptString(ref input) == Status.DECRYPTED)
                {
                    if (logging)
                    {
                        ShowDecryptedString(input);
                    }
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
                    {
                        ShowEncryptedString(input);
                    }
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
                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
            }
        }
        static private void ShowEncryptedString(string str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Encrypted string :");
            Console.WriteLine("START >");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("< END");
        }
        static private void ShowDecryptedString(string str)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Decrypted string :");
            Console.WriteLine("START >");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("< END");
        }
        static private void TransformFile(SmartLock smartLock, string filePath = "", bool logging = true)
        {//Encrypt/Descrypt file using the given smartLock instance
            if (smartLock == null) return;
            Console.Clear();
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
                Console.Write("Input file path: ");
                file = Console.ReadLine(); //Get file path
            }
            if (!File.Exists(file))
            {
                Console.WriteLine("File doesn't exist");
                return;
            }
            byte[] fileBytes = File.ReadAllBytes(file); //Read file bytes in memory
            if (smartLock.DataEncrypted(fileBytes))
            {
                if (smartLock.Decrypt(ref fileBytes) == Status.DECRYPTED)
                {
                    File.WriteAllBytes(file, fileBytes);
                }
            }
            else
            {
                if (smartLock.Encrypt(ref fileBytes) == Status.ENCRYPTED)
                {
                    File.WriteAllBytes(file, fileBytes);
                }
            }
            if (logging)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("Press enter to continue...");
                Console.ReadLine();
            }
        }

        private static void StatusChanged(UInt16 status)
        {//Event that triggers every time the smartLock instance status is changing
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\t" + Status.Descriptions[Status.language][status]);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception)
            {
                //Description not found
            }
        }
    }
}