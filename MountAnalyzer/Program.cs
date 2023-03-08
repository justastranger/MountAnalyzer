using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace MountAnalyzer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path;
            Dictionary<string, int> mountPriorities = new Dictionary<string, int>();

            if (args.Length != 0)
            {
                path = args[0];
            }
            else
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\WOW6432Node\\BioWare\\Mass Effect™ Legendary Edition"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("Install Dir");
                        if (value != null)
                        {
                            path = value.ToString() + "\\Game\\ME3\\BioGame\\DLC";
                        }
                        else
                        {
                            path = Directory.GetCurrentDirectory();
                        }
                    }
                    else
                    {
                        path = Directory.GetCurrentDirectory();
                    }
                }
            }
            if (Directory.Exists(path))
            {
                List<string> mountFiles = Directory.EnumerateFiles(path, "mount.dlc", SearchOption.AllDirectories).ToList();
                if (mountFiles.Count > 0)
                {
                    foreach (string mountFilePath in mountFiles)
                    {
                        string localPath = mountFilePath.Remove(0, path.Length);
                        var separators = new char[] {
                            Path.DirectorySeparatorChar,
                            Path.AltDirectorySeparatorChar
                        };
                        List<string> folders = localPath.Split(separators, StringSplitOptions.RemoveEmptyEntries).ToList();

                        using (FileStream mountFile = File.OpenRead(mountFilePath))
                        {
                            mountFile.Position = 0x10;
                            byte[] buffer = new byte[4];
                            mountFile.Read(buffer, 0, 4);
                            int mountPriority = BitConverter.ToInt32(buffer, 0);

                            mountPriorities.Add(folders[0], mountPriority);
                            // Console.WriteLine(folders[0] + ": " + mountPriority.ToString());
                        }

                    }
                    // serialize Dictionary

                    string serializedMountData = JsonConvert.SerializeObject(mountPriorities, Formatting.Indented);

                    Console.WriteLine(serializedMountData);
                    // find duplicates

                    var duplicates = mountPriorities.GroupBy(x => x.Value).Where(x => x.Count() > 1);
                    if (duplicates.Count() > 0) Console.WriteLine(JsonConvert.SerializeObject(duplicates, Formatting.Indented));
                    else Console.WriteLine("No duplicate mount priorities detected.");
                }
                else
                {
                    Console.WriteLine("Usage: MountAnalyzer.exe [pathToDLCFolder]");
                    Console.WriteLine("No 'mount.dlc' files found. Are you in the right folder?");
                }
            }
            else
            {
                Console.WriteLine("Usage: MountAnalyzer.exe [pathToDLCFolder]");
                Console.WriteLine("If pathToDLCFolder is not supplied, it will be assumed to be the working directory.");
                Console.WriteLine("The working directory is displayed on the left when using cmd on Windows.");
            }
        }
    }
}
