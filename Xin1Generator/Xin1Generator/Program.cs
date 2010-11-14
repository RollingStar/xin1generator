using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Xin1Generator {
    class Program {
        private const string chaptersName = "chapters.xml";
        private const string tagsName = "tags.xml";
        private const string qpfileName = "qpfile.txt";
        private const string demuxName = "demux.cmd";

        public static void Main(string[] args) {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine(assemblyName.Name + " v" + assemblyName.Version.ToString(2));

            try {
                foreach (string dependency in new string[] { "eac3to", "xport" }) {
                    try {
                        new Process {
                            StartInfo = {
                                FileName = dependency,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        }.Start();
                    } catch (Win32Exception e) {
                        throw new InvalidOperationException(e.Message + ": " + dependency);
                    }
                }

                Parameters p = new Parameters() {
                    titleNames = new string[0],
                    inPath = Directory.GetCurrentDirectory(),
                    outPath = Directory.GetCurrentDirectory(),
                    demuxTracks = false,
                    hideChapters = false
                };

                for (int i = 0; i < args.Length; i++) {
                    try {
                        switch (args[i]) {
                            case "-t":
                                p.titleNumbers = args[++i].Split(',');
                                break;
                            case "-n":
                                p.titleNames = args[++i].Split(',');
                                break;
                            case "-i":
                                p.inPath = args[++i];
                                break;
                            case "-o":
                                p.outPath = args[++i];
                                break;
                            case "-d":
                                p.demuxTracks = true;
                                break;
                            case "-h":
                                p.hideChapters = true;
                                break;
                            default:
                                throw new ParameterException(
                                    args[i] + " is not a valid switch");
                        }
                    } catch (IndexOutOfRangeException) {
                        throw new ParameterException(
                            "Switch " + args[i - 1] + " requires an argument");
                    }
                }

                if (p.titleNumbers == null)
                    throw new ParameterException("Title numbers not specified");

                foreach (string dir in new string[] { p.inPath, p.outPath })
                    if (!Directory.Exists(dir))
                        throw new DirectoryNotFoundException(
                            "Could not find directory " + dir);

                Console.WriteLine();

                Xin1Generator xin1Generator = new Xin1Generator(p);
                xin1Generator.ExtractInfo();
                xin1Generator.GenerateAll(chaptersName, tagsName, qpfileName, demuxName);
            } catch (ParameterException e) {
                Console.WriteLine();
                Console.WriteLine("Error: " + e.Message);
                Console.WriteLine();
                PrintUsage();
            } catch (Exception e) {
                Console.WriteLine();
                Console.WriteLine("Error: " + e.Message);
            }
        }

        public static void PrintUsage() {
            Console.WriteLine("Usage:");
            Console.WriteLine(" " + Environment.GetCommandLineArgs()[0] +
                " -t <titles> [-n <names>] [-i <input>] [-o <output>] [-d] [-h]");
            Console.WriteLine();
            Console.WriteLine("Parameters:");
            Console.WriteLine(" -t   " +
                "Comma-separated list of title numbers as shown by eac3to. Example: 2,1.");
            Console.WriteLine(" -n   " +
                "Comma-seperated list of title names. Example: \"Edition 1,Edition 2\".");
            Console.WriteLine(" -i   " +
                "Path to Blu-ray source disc. Default: current directory.");
            Console.WriteLine(" -o   " +
                "Path to destination directory. Default: current directory.");
            Console.WriteLine(" -d   " +
                "Demux tracks immediately instead of writing the command for it to a file.");
            Console.WriteLine(" -h   " +
                "Set chapters to hidden. Prevents overly long chapter lists.");
        }
    }
}
