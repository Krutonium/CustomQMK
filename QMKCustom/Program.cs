using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

using LibGit2Sharp;

namespace QMKCustom
{
    class Program
    {
        private static readonly string repoLocation = "./qmk_firmware";

        private static readonly string k556_config_h = "./qmk_firmware/keyboards/redragon/k556/config.h";
        private static readonly string k556_rules_mk = "./qmk_firmware/keyboards/redragon/k556/rules.mk";
        private static readonly string k552_rev1_config_h = "./qmk_firmware/keyboards/redragon/k552/rev2/config.h";
        private static readonly string k552_rev1_rules_mk = "./qmk_firmware/keyboards/redragon/k552/rev2/rules.mk";
        private static readonly string k552_rev2_config_h = "./qmk_firmware/keyboards/redragon/k552/rev2/config.h";
        private static readonly string k552_rev2_rules_mk = "./qmk_firmware/keyboards/redragon/k552/rev2/rules.mk";
        
        
        private static readonly string openrgb_branch = "sn32_openrgb";
        static void Main(string[] args)
        {
            if (Directory.Exists(repoLocation))
            {
                Directory.Delete(repoLocation, true);
            }

            CloneOptions options = new CloneOptions();
            options.RecurseSubmodules = true;
            options.BranchName = openrgb_branch;
            Console.WriteLine("Cloning Repo...");
            Repository.Clone("https://github.com/SonixQMK/qmk_firmware.git", repoLocation, options);
            Console.WriteLine("Adding Different Debounce");
            WebClient wc = new WebClient();
            wc.DownloadFile("https://raw.githubusercontent.com/smp4488/qmk_firmware/981de4a14c91d4d016c6e71a6b5fbba0a8eb8d11/quantum/debounce/sym_eager_g.c",
                repoLocation + "/quantum/debounce/sym_eager_g.c");
            Console.WriteLine("Editing Config.h");
            //EditConfigH(k552_rev1_config_h);   
            //EditConfigH(k552_rev2_config_h);
            EditConfigH(k556_config_h);
            Console.WriteLine("Done Editing Config.h; Editing rules.mk");
            //EditRulesMk(k552_rev1_rules_mk);
            //EditRulesMk(k552_rev2_rules_mk);
            EditRulesMk(k556_rules_mk);
            Console.WriteLine("Finished Editing rules.mk");
            Console.WriteLine("Starting Build...");
            BuildFirmware();
        }

        private static void BuildFirmware()
        {
            //make redragon/k556 -j$(nproc --all) 
            ProcessStartInfo info = new ProcessStartInfo();
            info.WorkingDirectory = repoLocation;
            info.FileName = "/usr/bin/make";
            info.Arguments = "redragon/k552/rev1 -j" + Environment.ProcessorCount;
            //Process.Start(info).WaitForExit();
            info.Arguments = "redragon/k552/rev2 -j" + Environment.ProcessorCount;
            //Process.Start(info).WaitForExit();
            info.Arguments = "redragon/k556 -j" + Environment.ProcessorCount;
            Process.Start(info).WaitForExit();
            
           // File.Move(repoLocation + "/redragon_k552_rev1_default.bin", "./redragon_k552_rev1_default.bin");
           // File.Move(repoLocation + "/redragon_k552_rev2_default.bin", "./redragon_k552_rev2_default.bin");
           if (File.Exists("./redragon_k556_default.bin")) {
               File.Delete("./redragon_k556_default.bin");
           }
            File.Move(repoLocation + "/redragon_k556_default.bin", "./redragon_k556_default.bin");
            Directory.Delete(repoLocation, true);
            Console.WriteLine("All Done!");
        }
        

        public static void EditRulesMk(string path)
        {
            Console.WriteLine("Editing " + path);
            string RulesMk = File.ReadAllText(path);
            RulesMk = EditLine(RulesMk, "NKRO_ENABLE", "no", "yes");
            RulesMk = EditLine(RulesMk, "SLEEP_LED_ENABLE", "yes", "no");
            RulesMk = injectLine(RulesMk, "DEBOUNCE_TYPE = sym_eager_g", "RGB_MATRIX_DRIVER");
            File.WriteAllText(path, RulesMk);
        }

        public static string EditLine(string oldText, string variable, string oldValue, string newValue)
        {
            Console.WriteLine("Setting " + variable + " to " + newValue);
            bool success = false;
            List<string> lines = oldText.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            ).ToList();
            List<string> finishedText = new List<string>();
            foreach (var line in lines)
            {
                var newText = line;
                if (line.Contains(variable))
                {
                    newText = line.Replace(oldValue, newValue);
                    success = true;
                }
                finishedText.Add(newText);
            }
            string toReturn = "";
            foreach (var line in finishedText)
            {
                toReturn += line + Environment.NewLine;
            }
            Debug.Assert(success);
            return toReturn;
        }
        
        public static void EditConfigH(string path)
        {
            Console.WriteLine("Editing " + path);
            string newFile = injectLine(File.ReadAllText(path), "#define FORCE_NKRO", "#define DEBOUNCE");
            newFile = injectLine(newFile, "#define SLEEP_LED_MODE_ANIMATION RGB_MATRIX_NONE", "#define FORCE_NKRO");
            newFile = injectLine(newFile, "/* Enable NKRO and Disable Sleep RGB */", "#define DEBOUNCE");
            newFile = removeLine(newFile, "#define DEBOUNCE");
            newFile = injectLine(newFile, "#define DEBOUNCE 5", "#define FORCE_NKRO");
            File.WriteAllText(path, newFile);
        }

        public static string removeLine(string oldText, string toRemove)
        {
            Console.WriteLine("Removing " + toRemove);
            List<string> lines = oldText.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            ).ToList();
            List<string> newText = new List<string>();
            foreach (var line in lines)
            {
                if (!line.Contains(toRemove))
                {
                    newText.Add(line);
                }
            }
            string toReturn = "";
            foreach (var line in newText)
            {
                toReturn += line + Environment.NewLine;
            }
            return toReturn;
        }
        
        public static string injectLine(string oldText, string newText, string after)
        {
            Console.WriteLine("Injecting " + newText);
            bool succcess = false;
            List<string> lines = oldText.Split(
                new[] {"\r\n", "\r", "\n"},
                StringSplitOptions.None
            ).ToList();
            //List of Strings
            
            List<string> finishedText = new List<string>();
            //Our Edited File
            
            foreach (var line in lines)
            {
                finishedText.Add(line);
                if (line.Contains(after))
                {
                    finishedText.Add(newText); //Inject our new line
                    succcess = true;
                }
            }

            string toReturn = "";
            foreach (var line in finishedText)
            {
                toReturn += line + Environment.NewLine;
            }
            Debug.Assert(succcess);
            return toReturn;
        }
    }
}