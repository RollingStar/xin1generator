using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    static class XportWrapper {
        public const string processFileName = "xport";

        private static ProcessStartInfo GetStartInfo() {
            return new ProcessStartInfo {
                FileName = processFileName,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
        }

        public static int GetFrameCount(string path) {
            string output;

            using (var process = new Process { StartInfo = GetStartInfo() }) {
                process.StartInfo.Arguments = "-psh \"" + path + "\" 1 1 0";
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            Match frameCountMatch = Regex.Match(output, @"coded pictures = (\d+)");

            if (!frameCountMatch.Success)
                throw new InvalidOperationException(
                    "Could not determine frame count of " + path);

            return int.Parse(frameCountMatch.Groups[1].Value);
        }
    }
}
