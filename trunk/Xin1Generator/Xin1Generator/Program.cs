using System;
using System.Collections.Generic;
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
            Trace.WriteLine(string.Format(Properties.Resources.NameAndVersionFormat,
                assemblyName.Name, assemblyName.Version.ToString(2)));

            try {
                Utilities.CheckDependencies();

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
                            case "-p":
                                p.PreserveChapters = true;
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

                if (titleNumbers.Count == 0)
                    throw new ParameterException("Title numbers not specified");

                if (titleNames.Count > 0 && titleNames.Count != titleNumbers.Count)
                    throw new ParameterException("Incorrect number of title names");

                for (int i = 0; i < titleNumbers.Count; i++)
                    p.Titles.Add(new Title() {
                        Number = titleNumbers[i],
                        Name = titleNames.Count > 0 ? titleNames[i] : "Edition " + (i + 1)
                    });

                foreach (string dir in new[] { p.InputPath, p.OutputPath })
                    if (!Directory.Exists(dir))
                        throw new DirectoryNotFoundException("Could not find directory " + dir);

                Trace.WriteLine(string.Empty);

                var xin1Generator = new Xin1Generator(p);
                xin1Generator.ExtractAll();
                xin1Generator.GenerateAll();
            } catch (ParameterException e) {
                Trace.WriteLine(string.Empty);
                Trace.WriteLine(string.Format(Properties.Resources.ErrorMessage, e.Message));
                Trace.WriteLine(string.Empty);
                PrintUsage();
            } catch (Exception e) {
                Trace.WriteLine(string.Empty);
                Trace.WriteLine(string.Format(Properties.Resources.ErrorMessage, e.Message));
            }
        }

        public static void PrintUsage() {
            Trace.WriteLine(string.Format(
                Properties.Resources.UsageText, Environment.GetCommandLineArgs()[0]));
        }
    }
}
