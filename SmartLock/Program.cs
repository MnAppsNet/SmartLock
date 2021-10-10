using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace SmartLock
{
    class Commands
    {//Commands when application is run with arguments
        public const string file = "-f";
        public static readonly string fileDescription = AppSettings.Texts.Get(AppTexts.COMMAND_F);
        public const string text = "-t";
        public static readonly string textDescription = AppSettings.Texts.Get(AppTexts.COMMAND_T);
        public const string decrypt = "-d";
        public static readonly string decrypttDescription = AppSettings.Texts.Get(AppTexts.COMMAND_D);
        public const string help = "-h";
        public static readonly string helpDescription = AppSettings.Texts.Get(AppTexts.COMMAND_H);
    }

    class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static async Task Main(string[] args)
        {//<<= Entry point =>>
            if (args.Length == 0)
            {//Normal execution
                await ShowMenu();
            }
            else
            {//Execution with arguments
                //Hide console :
                IntPtr h = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                ShowWindow(h, 0);
                //Handle commands :
                await HandleCommands(args);
            }
        }

        static private async Task ShowMenu()
        {//Show main menu with all the available options
            SmartLock smartLock = await Functions.InitializeSmartLock(true,StatusChanged);
            bool exit = false;
            do
            {
                UInt16 option = ShowOptions();
                switch (option)
                {
                    case 1://Transform File
                        Functions.TransformFile(smartLock);
                        break;
                    case 2://Transform string
                        Functions.TransformString(smartLock);
                        break;
                    case 3://Decrypt with user password
                        Functions.DecryptWithPassword();
                        break;
                    case 4://Show settings
                        Settings();
                        break;
                    default://Exit
                        exit = true;
                        break;
                }
            } while (!exit);
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(AppSettings.Texts.Get(AppTexts.PRESS_ENTER_TO_EXIT));
            Console.ReadLine();
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
                Console.WriteLine(AppSettings.Texts.Get(AppTexts.OPTIONS));
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("\t1." + AppSettings.Texts.Get(AppTexts.TRANSFORM_FILE));
                Console.WriteLine("\t2."+ AppSettings.Texts.Get(AppTexts.TRANSFORM_STRING));
                Console.WriteLine("\t3." + AppSettings.Texts.Get(AppTexts.DECRYPT_WITH_PASS));
                Console.WriteLine("\t4."+ AppSettings.Texts.Get(AppTexts.SETTINGS));
                Console.WriteLine("\t5." + AppSettings.Texts.Get(AppTexts.EXIT));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(AppSettings.Texts.Get(AppTexts.SELECT_OPTION) + " ");
                string userInput = Console.ReadLine();
                if (userInput.Length > 0)
                    option = userInput.Substring(0, 1);
                else
                    option = "0";
            } while (!(validOptions.Contains(option + ".")));

            return UInt16.Parse(option);
        }
        static private void Settings()
        {
            bool exit = false;
            do
            {
                UInt16 option = ShowSettings();
                switch (option)
                {
                    case 1:
                        Functions.HandleContextMenuAction();
                        break;
                    case 2:
                        AppSettings.Properties.ReplaceOriginalFile = !AppSettings.Properties.ReplaceOriginalFile;
                        AppSettings.Save();
                        break;
                    case 3:
                        Functions.HandleFileExtensionAssociation();
                        break;
                    default:
                        exit = true;
                        break;
                }
            } while (!exit);
        }
        static private UInt16 ShowSettings()
        {
            string validOptions = "1.2.3.4.";
            string option;
            string ContectMenu = ((Functions.ContectMenuActionActive()) ? AppSettings.Texts.Get(AppTexts.DISABLE) : AppSettings.Texts.Get(AppTexts.ENABLE)) + AppSettings.Texts.Get(AppTexts.CONTEXT_MENU);
            string ReplaceOriginalFile = ((AppSettings.Properties.ReplaceOriginalFile) ? AppSettings.Texts.Get(AppTexts.DONT_REPLACE) : AppSettings.Texts.Get(AppTexts.REPLACE)) + " " + AppSettings.Texts.Get(AppTexts.FILE_AFTER_TRANSFORMATION);
            string ExtensionAssiciation = ((Functions.FileExtensionAssociated()) ? AppSettings.Texts.Get(AppTexts.DISSOCIATE) : AppSettings.Texts.Get(AppTexts.ASSSOCIATE)) + " " + AppSettings.Texts.Get(AppTexts.FILE_EXTENSION) + " '" + AppSettings.EncryptedFileExtention + "'";
            do
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(AppSettings.Texts.Get(AppTexts.SETTINGS));
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("----------------------------------------");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine("\t1." + ContectMenu);
                Console.WriteLine("\t2." + ReplaceOriginalFile);
                Console.WriteLine("\t3." + ExtensionAssiciation);
                Console.WriteLine("\t4." + AppSettings.Texts.Get(AppTexts.BACK));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(AppSettings.Texts.Get(AppTexts.SELECT_OPTION));
                string userInput = Console.ReadLine();
                if (userInput.Length > 0)
                    option = userInput.Substring(0, 1);
                else
                    option = "0";
            } while (!(validOptions.Contains(option + ".")));
            return UInt16.Parse(option);
        }
        private static async Task HandleCommands(string[] args)
        {//Handle execution with arguments

            if (args.Length >= 2)
            {
                SmartLock smartLock = await Functions.InitializeSmartLock(false, StatusChanged);
                switch (args[0].ToLower())
                {
                    case Commands.file:
                        Functions.TransformFile(smartLock, args[1], false);
                        break;
                    case Commands.text:
                        Functions.TransformString(smartLock, args[1], false);
                        break;
                    case Commands.decrypt:
                        if (args.Length < 3)
                            ShowHelp();
                        else
                            Functions.DecryptWithPassword(args[1], args[2], false);
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
                    SmartLock smartLock = await Functions.InitializeSmartLock(false, StatusChanged);
                    Functions.TransformFile(smartLock, args[0], false);
                }
            }
        }
        private static void ShowHelp()
        {//Show help menu with all available commands
            List<FieldInfo> options = typeof(Commands).GetFields()
                                        .Where(fi => fi.IsInitOnly).ToList();
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach (FieldInfo fi in options)
            {
                if(fi.GetValue(null).ToString().Length > 2) //Show only descriptions
                    Console.WriteLine((string)fi.GetValue(null));
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void StatusChanged(string status)
        {//Event that triggers every time the smartLock instance status is changing
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\t" + Status.Get(status));
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception)
            {
                //Description not found
            }
        }
    }
}