using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    static class Eac3toWrapper {
        public const string processFileName = "eac3to";

        private static Process process = new Process {
            StartInfo = {
                FileName = processFileName,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        public static IDictionary<string, Title> GetTitles(string workingDirectory) {
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            MatchCollection matches = Regex.Matches(output,
                @"(\d+)\) \d+.+?(\S+(\.\w+)).+?([pi])(\d+)(?: \/(\d+\.\d+))?",
                RegexOptions.Singleline);
            IDictionary<string, Title> titles = new Dictionary<string, Title>();

            foreach (Match match in matches) {
                var title = new Title() { files = new List<string>() };

                string ext = match.Groups[3].Value;
                string files = match.Groups[2].Value
                    .Replace(ext, string.Empty).Trim(new[] { '[', ']' });

                foreach (string file in files.Split('+'))
                    title.files.Add(Path.Combine(workingDirectory,
                        "BDMV", "STREAM", int.Parse(file).ToString("D5") + ext));

                int num = int.Parse(match.Groups[5].Value);
                double den = 1;
                double.TryParse(match.Groups[6].Value, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out den);

                // Treat 2 fields as 1 frame (does this actually work?)
                title.frameRate = num / den * (match.Groups[4].Value == "i" ? 2 : 1);

                titles.Add(match.Groups[1].Value, title);
            }

            return titles;
        }

        public static void WriteTracks(string workingDirectory, string arguments) {
            process.StartInfo.Arguments = arguments + " -progressnumbers";
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.Start();

            string line;
            bool isAnalyzing = true;

            Trace.Indent();

            while (!string.IsNullOrEmpty(line = process.StandardOutput.ReadLine())) {
                if (!line.EndsWith("%"))
                    continue;

                // Add newline after analyzing
                if (isAnalyzing != (isAnalyzing = line.StartsWith("analyze")))
                    Trace.WriteLine(string.Empty);

                // Overwrite current line
                Trace.Write('\r' + new string(' ', Trace.IndentSize) +
                    (isAnalyzing ? "Analyzing" : "Processing") + ": " +
                    line.Substring(line.IndexOf(' ') + 1));
            }

            Trace.Unindent();

            process.WaitForExit();
        }
    }
}
