using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltSeeker
{
    internal class Program
    {
        static readonly string[] VOLT_EXTENSIONS = new string[] { "vol", "avl" };

        static void Main(string[] args)
        {
            if (args.Length == 0) {
                Console.WriteLine($"Please supply the path of your game that contains the {string.Join("/", VOLT_EXTENSIONS)} files!");
                Console.ReadLine();
                Environment.Exit(-1);
            }
            else {
                //List<string> bigBExtensions = new List<string>() { "LCS", "LCM" };

                string dirPath = args[0];
                var files = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in files) {
                    bool skip = true;
                    foreach (var ext in VOLT_EXTENSIONS) {
                        if (file.EndsWith(ext)) {
                            skip = false;
                            break;
                        }
                    }

                    if (skip) continue;

                    var arc = new VoltArchive(file);

                    Console.WriteLine(arc);

                    arc.SaveToDirectory(Path.Combine(dirPath, $"{Path.GetFileName(file)}_extracted"));
                }

                Console.WriteLine("Done!");
                Console.ReadLine();
            }
        }
    }
}
