using System.Diagnostics;
using System.Reflection;

namespace portableLaunch
{
    internal class Program
    {
        static int Main(string[] args)
        {
            String inifile = "";

            if (args.Length >= 1)
                inifile = Environment.ExpandEnvironmentVariables(args[0]).Trim();

            if (inifile == "")
                inifile = Environment.ExpandEnvironmentVariables("portable.ini").Trim();

            if (!File.Exists(inifile))
            {
                Console.WriteLine("File \"" + inifile + "\" not found");
                return 1;
            }
            Console.WriteLine("Reading \"" + inifile + "\"");

            Ini ini = new(inifile);

            String launchDir = Environment.ExpandEnvironmentVariables(ini.Read("launchDir", "general")).Trim();
            String saveRoot = Environment.ExpandEnvironmentVariables(ini.Read("saveRoot", "general")).Trim();
            String saveDirs = Environment.ExpandEnvironmentVariables(ini.Read("saveDirs", "general")).Trim();
            String exe = Environment.ExpandEnvironmentVariables(ini.Read("exe", "general")).Trim();

            if (exe == "")
            {
                Console.WriteLine("Error: exe not set in \"" + inifile + "\"");
                return 2;
            }

            if (saveRoot == "")
                saveRoot = "save";

            if (launchDir == "")
                launchDir = ".";

            Console.WriteLine("saveDirs: \"" + saveDirs + "\"");
            Console.WriteLine("launchDir: \"" + launchDir + "\"");
            Console.WriteLine("exe: \"" + exe + "\"");

            var startInfo = new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = launchDir
            };

            if (!Directory.Exists(startInfo.WorkingDirectory))
            {
                Console.WriteLine("launchDir \"" + startInfo.WorkingDirectory + "\" does not exist.");
                return 3;
            }

            if (!File.Exists(startInfo.FileName))
            {
                Console.WriteLine("exe \"" + startInfo.FileName + "\" does not exist.");
                return 4;
            }

            if (saveDirs == "")
            {
                Console.WriteLine("Launching \"" + startInfo.WorkingDirectory + "\\" + startInfo.FileName + "\"");
                Process.Start(startInfo)?.WaitForExit();
            }
            string[] saveDirsArray = saveDirs.Split(',').Select(sValue => sValue.Trim()).ToArray();

            if (!Directory.Exists(saveRoot))
            {
                Console.WriteLine("Creating portable save directory \"" + saveRoot + "\"");
                Directory.CreateDirectory(saveRoot);

                for (int i = 0; i < saveDirsArray.Length; i++)
                {
                    string[] path = saveDirsArray[i].Split([':'], 2);
                    if (Directory.Exists(path[1]))
                    {
                        Console.WriteLine("Copying local savedata from \"" + path[1] + "\" to portable save directory \"" + saveRoot + path[0] + "\"");
                        Process.Start("xcopy.exe", "\"" + path[1] + "\"" + " \"" + saveRoot + path[0] + "\"")?.WaitForExit();
                    }
                }
            }

            for (int i = 0; i < saveDirsArray.Length; i++)
            {
                string[] path = saveDirsArray[i].Split(new[] { ':' }, 2);
                
                if (Directory.Exists(path[1]))
                {
                    if (Directory.Exists(path[1] + ".bak"))
                        Directory.Delete(path[1] + ".bak");
                    Console.WriteLine("Backing up local savedata \"" + path[1] + "\" as \"" + path[1] + ".bak\"");
                    Directory.Move(path[1], path[1] + ".bak");
                }

                Console.WriteLine("Creating symlink \"" + path[1] + "\" to \"" + saveRoot + path[0] + "\"");
                
                var parent = Directory.GetParent(path[1])?.FullName;
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                
                Directory.CreateSymbolicLink(path[1], saveRoot + path[0]);
            }

            Console.WriteLine("Launching \"" + startInfo.WorkingDirectory + "\\" + startInfo.FileName + "\"");
            Process.Start(startInfo)?.WaitForExit();

            for (int i = 0; i < saveDirsArray.Length; i++)
            {
                string[] path = saveDirsArray[i].Split(new[] { ':' }, 2);
                Console.WriteLine("Removing symlink \"" + path[1] + "\" to \"" + saveRoot + path[0] + "\"");
                Directory.Delete(path[1]);
                if (Directory.Exists(path[1] + ".bak"))
                {
                    Console.WriteLine("Restoring backup local savedata \"" + path[1] + "\" from \"" + path[1] + ".bak\"");
                    Directory.Move(path[1] + ".bak", path[1]);
                }
            }

            return 0;
        }
    }
}