﻿using System.Diagnostics;

namespace portableLaunch
{
    internal class Program
    {
        static int Main(string[] args)
        {
            String inifile = "";
            String arg = "";
            
            if (args.Length >= 1) {
                arg = Environment.ExpandEnvironmentVariables(args[0]).Trim();
                if (File.Exists(arg))
                    inifile = arg;
                else if (Directory.Exists(arg))
                    inifile = IniSelector(arg);
                else
                {
                    Console.WriteLine(arg + "not found.");
                    return 1;
                }
            }
            else {
                if (Directory.Exists(Environment.ExpandEnvironmentVariables("portable").Trim()))
                    inifile = IniSelector(Environment.ExpandEnvironmentVariables("portable").Trim());
                else if (File.Exists(Environment.ExpandEnvironmentVariables("portable.ini").Trim()))
                    inifile = Environment.ExpandEnvironmentVariables("portable.ini").Trim();
                else
                {
                    Console.WriteLine("No portable file or folder found.");
                    return 1;
                }
            }

            if (inifile == "")
            {
                Console.WriteLine("No ini file selected.");
                return 1;
            }

            inifile = Path.GetFullPath(inifile);

            if (!File.Exists(inifile)) {
                Console.WriteLine("Error: \"" + inifile + "\" not found.");
                return 1;
            }

            Console.WriteLine("Reading \"" + inifile + "\"");

            String directory = Path.TrimEndingDirectorySeparator(Path.GetDirectoryName(inifile));
            Directory.SetCurrentDirectory(directory!);

            Ini.Ini ini = new(inifile);

            String launchDir = Environment.ExpandEnvironmentVariables(ini.Read("launchDir", "general")).Trim();
            String saveRoot = Environment.ExpandEnvironmentVariables(ini.Read("saveRoot", "general")).Trim();
            String saveDirs = Environment.ExpandEnvironmentVariables(ini.Read("saveDirs", "general")).Trim();
            String exe = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Environment.ExpandEnvironmentVariables(ini.Read("exe", "general")).Trim()));

            if (exe == "")
            {
                Console.WriteLine("Error: exe not set in \"" + inifile + "\"");
                return 2;
            }

            if (saveRoot == "")
                saveRoot =  Path.Combine("saves", Path.GetFileNameWithoutExtension(inifile));

            if (launchDir == "")
            {
                if (File.Exists(exe))
                    launchDir = Path.GetDirectoryName(exe)!;
                else
                    launchDir = ".";
            }

            saveRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(saveRoot));
            launchDir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(launchDir));

            if (saveDirs != "")
            {
                Console.WriteLine("saveRoot: \"" + saveRoot + "\"");
                Console.WriteLine("saveDirs: \"" + saveDirs + "\"");
            }
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
                Console.WriteLine("Launching \"" + startInfo.FileName + "\"");
                Process.Start(startInfo)?.WaitForExit();
                return 0;
            }

            string[] saveDirsArray = saveDirs.Split(',').Select(sValue => sValue.Trim()).ToArray();

            if (!Directory.Exists(saveRoot))
            {
                Console.WriteLine("Creating portable save directory \"" + saveRoot + "\"");
                Directory.CreateDirectory(saveRoot);

                for (int i = 0; i < saveDirsArray.Length; i++)
                {
                    string[] path = saveDirsArray[i].Split(new[] { ':' }, 2);
                    path[0] = path[0].Substring(1);
                    
                    path[0] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(saveRoot, path[0])));
                    path[1] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path[1]));

                    if (Directory.Exists(path[1]))
                    {
                        Console.WriteLine("Copying local savedata from \"" + path[1] + "\" to portable save directory \"" + path[0] + "\"");
                        Process.Start("xcopy.exe", "\"" + path[1] + "\"" + " \"" + path[0] + "\"")?.WaitForExit();
                    }
                    else if (File.Exists(path[1]))
                    {
                        Console.WriteLine("Copying local savedata from \"" + path[1] + "\" to portable save directory \"" + path[0] + "\"");
                        Process.Start("copy", "\"" + path[1] + "\"" + " \"" + path[0] + "\"")?.WaitForExit();
                    }
                }
            }

            for (int i = 0; i < saveDirsArray.Length; i++)
            {
                string[] path = saveDirsArray[i].Split(new[] { ':' }, 2);
                path[0] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(saveRoot, path[0])));
                path[1] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path[1]));
                String backup = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path[1] + ".bak"));

                if (Directory.Exists(path[1]))
                {
                    if (Directory.Exists(backup))
                        Directory.Delete(backup);
                    Console.WriteLine("Backing up local savedata \"" + path[1] + "\" as \"" + backup + "\"");
                    Directory.Move(path[1], backup);
                }
                else if (File.Exists(path[1]))
                {
                    if (File.Exists(backup))
                        File.Delete(backup);
                    Console.WriteLine("Backing up local savedata \"" + path[1] + "\" as \"" + backup + "\"");
                    File.Move(path[1], backup);
                }

                Console.WriteLine("Creating symlink \"" + path[1] + "\" to \"" + path[0] + "\"");

                var parent = Directory.GetParent(path[1])?.FullName;
                if (parent != null && !Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }

                if (Directory.Exists(path[0]))
                    Directory.CreateSymbolicLink(path[1], path[0]);
                else if (File.Exists(path[0]))
                    File.CreateSymbolicLink(path[1], path[0]);
            }

            Console.WriteLine("Launching \"" + startInfo.FileName + "\"");
            if (ini.Read("wait", "general") == "false")
                Process.Start(startInfo);
            else
                Process.Start(startInfo)?.WaitForExit();

            for (int i = 0; i < saveDirsArray.Length; i++)
            {
                string[] path = saveDirsArray[i].Split(new[] { ':' }, 2);
                path[0] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(saveRoot, path[0])));
                path[1] = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path[1]));
                String backup = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path[1] + ".bak"));

                Console.WriteLine("Removing symlink \"" + path[1] + "\" to \"" + path[0] + "\"");

                if (Directory.Exists(path[1]))
                {
                    Directory.Delete(path[1]);
                    if (Directory.Exists(backup))
                    {
                        Console.WriteLine("Restoring backup local savedata \"" + path[1] + "\" from \"" + backup + "\"");
                        Directory.Move(backup, path[1]);
                    }
                }
                else if (File.Exists(path[1]))
                {
                    File.Delete(path[1]);
                    if (File.Exists(backup))
                    {
                        Console.WriteLine("Restoring backup local savedata \"" + path[1] + "\" from \"" + backup + "\"");
                        File.Move(backup, path[1]);
                    }
                }
            }

            return 0;
        }

        static String IniSelector(String dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.SetCurrentDirectory(dir);
                List<String> options = Directory.GetFiles(".\\", "*.ini").ToList();

                for (int i = 0; i < options.Count; i++)
                {
                    Ini.Ini ini = new(options[i]);
                    ini.Read("exe", "general");

                    String exe = ini.Read("exe", "general");

                    if (exe == "" || !File.Exists(exe))
                    {
                        options.RemoveAt(i--);
                    }
                }

                if (options.Count <= 0)
                {
                    Console.WriteLine("No ini files found in " + dir);
                    return "";
                }

                int currentSelection = 0;

                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("Use arrow keys to navigate and press Enter to select:");

                    // Display options
                    for (int i = 0; i < options.Count; i++)
                    {
                        if (i == currentSelection)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"> {Path.GetFileNameWithoutExtension(options[i])}");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine($"  {Path.GetFileNameWithoutExtension(options[i])}");
                        }
                    }

                    // Capture key press
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.UpArrow)
                    {
                        currentSelection = (currentSelection == 0) ? options.Count - 1 : currentSelection - 1;
                    }
                    else if (key.Key == ConsoleKey.DownArrow)
                    {
                        currentSelection = (currentSelection == options.Count - 1) ? 0 : currentSelection + 1;
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.Clear();
                        return options[currentSelection];
                    }
                }
            }
            return "";
        }
    }
}