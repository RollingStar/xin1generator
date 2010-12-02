using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    static class XportWrapper {
        public const string processFileName = "xport";

        public static Process process = new Process {
            StartInfo = {
                FileName = processFileName,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        public static int GetFrameCount(string workingDirectory, string path) {
            Trace.Indent();
            Trace.WriteLine(Path.GetFileName(path));
            Trace.Unindent();

            process.StartInfo.Arguments = "-psh \"" + path + "\" 1 1 0";
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Match frameCountMatch = Regex.Match(output, @"coded pictures = (\d+)");

            if (string.IsNullOrEmpty(frameCountMatch.Value))
                throw new InvalidOperationException(
                    "Could not determine frame count of " + path);

            return int.Parse(frameCountMatch.Groups[1].Value);
        }
    }
}
