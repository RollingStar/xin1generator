using System;
using System.Collections.Generic;
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
            Console.WriteLine(Properties.Resources.NameAndVersionFormat,
                assemblyName.Name, assemblyName.Version.ToString(2));

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
                    TitleNumbers = new List<string>(),
                    TitleNames = new List<string>(),
                    InPath = Directory.GetCurrentDirectory(),
                    OutPath = Directory.GetCurrentDirectory(),
                    DemuxTracks = false,
                    HideChapters = false
                };

                for (int i = 0; i < args.Length; i++) {
                    try {
                        switch (args[i]) {
                            case "-t":
                                p.TitleNumbers.AddRange(args[++i].Split(','));
                                break;
                            case "-n":
                                p.TitleNames.AddRange(args[++i].Split(','));
                                break;
                            case "-i":
                                p.InPath = args[++i];
                                break;
                            case "-o":
                                p.OutPath = args[++i];
                                break;
                            case "-d":
                                p.DemuxTracks = true;
                                break;
                            case "-h":
                                p.HideChapters = true;
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

                if (p.TitleNumbers.Count == 0)
                    throw new ParameterException("Title numbers not specified");

                foreach (string dir in new string[] { p.InPath, p.OutPath })
                    if (!Directory.Exists(dir))
                        throw new DirectoryNotFoundException(
                            "Could not find directory " + dir);

                Console.WriteLine();

                Xin1Generator xin1Generator = new Xin1Generator(p);
                xin1Generator.ExtractInfo();
                xin1Generator.GenerateAll(chaptersName, tagsName, qpfileName, demuxName);
            } catch (ParameterException e) {
                Console.WriteLine();
                Console.WriteLine(Properties.Resources.ErrorPrefix + e.Message);
                Console.WriteLine();
                PrintUsage();
            } catch (Exception e) {
                Console.WriteLine();
                Console.WriteLine(Properties.Resources.ErrorPrefix + e.Message);
            }
        }

        public static void PrintUsage() {
            Console.WriteLine(Properties.Resources.UsageText,
                Environment.GetCommandLineArgs()[0]);
        }
    }
}
