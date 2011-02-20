using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xin1Generator {
    class Program {
        public static void Main(string[] args) {
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.IndentSize = 1;

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            Console.WriteLine(Properties.Resources.NameAndVersionFormat,
                assemblyName.Name, assemblyName.Version.ToString(2));

            try {
                foreach (string dependency in new[] { "eac3to", "xport" }) {
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

                var titleNumbers = new List<int>();
                var titleNames = new List<String>();

                var p = new Parameters {
                    InputPath = Directory.GetCurrentDirectory(),
                    OutputPath = Directory.GetCurrentDirectory()
                };

                for (int i = 0; i < args.Length; i++) {
                    try {
                        switch (args[i]) {
                            case "-t":
                                titleNumbers.AddRange(args[++i].Split(',')
                                    .Select(x => int.Parse(x)));
                                break;
                            case "-n":
                                titleNames.AddRange(args[++i].Split(','));
                                break;
                            case "-i":
                                p.InputPath = args[++i];
                                break;
                            case "-o":
                                p.OutputPath = args[++i];
                                break;
                            case "-d":
                                p.ExtractTracks = true;
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

                for (int i = 0; i < titleNumbers.Count; i++)
                    p.Titles.Add(new Title() {
                        Number = titleNumbers[i],
                        Name = i < titleNames.Count ? titleNames[i] : "Edition " + (i + 1)
                    });

                if (p.Titles.Count == 0)
                    throw new ParameterException("Title numbers not specified");

                foreach (string dir in new[] { p.InputPath, p.OutputPath })
                    if (!Directory.Exists(dir))
                        throw new DirectoryNotFoundException(
                            "Could not find directory " + dir);

                Console.WriteLine();

                var xin1Generator = new Xin1Generator(p);
                xin1Generator.ExtractInfo();
                xin1Generator.GenerateAll();
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
