using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    public static class Eac3toWrapper {
        public const string processFileName = "eac3to";

        private static ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = processFileName,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        public static List<Title> GetTitles(string workingDirectory) {
            var process = new Process { StartInfo = startInfo };
            process.StartInfo.Arguments = string.Empty;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var titles = new List<Title>();

            MatchCollection titleMatches =
                Regex.Matches(output, @"(\d+)\) (\d.+?(?:\n[\s\b]*\n|.$))", RegexOptions.Singleline);

            foreach (Match titleMatch in titleMatches) {
                var title = new Title();

                Match lengthMatch = Regex.Match(titleMatch.Groups[2].Value, @"(?:\d+\:){2}\d+");
                Match filesMatch = Regex.Match(titleMatch.Groups[2].Value, @"\S+(\.m2ts)");
                Match frameRateMatch =
                    Regex.Match(titleMatch.Groups[2].Value, @"([pi])(\d+)(?: \/(\d+\.\d+))?");

                string ext = filesMatch.Groups[1].Value;
                string files =
                    filesMatch.Value.Replace(ext, string.Empty).Trim(new[] { '[', ']' });
                int num = int.Parse(frameRateMatch.Groups[2].Value);
                double den = 1;
                double.TryParse(frameRateMatch.Groups[3].Value,
                    NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out den);

                title.Number = int.Parse(titleMatch.Groups[1].Value);
                title.Length = lengthMatch.Value;
                title.FrameRate = num / den / (frameRateMatch.Groups[1].Value == "i" ? 2 : 1);

                foreach (string file in files.Split('+'))
                    title.Files.Add(Path.Combine(workingDirectory,
                        "BDMV", "STREAM", int.Parse(file).ToString("D5") + ext));

                titles.Add(title);
            }

            return titles;
        }

        public static List<Track> GetTracks(string workingDirectory, string arguments) {
            var process = new Process { StartInfo = startInfo };
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingDirectory;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var tracks = new List<Track>();

            MatchCollection trackMatches =
                Regex.Matches(output, @"(\d+): (.+?), ([A-Z][a-z]+)?");

            foreach (Match trackMatch in trackMatches)
                tracks.Add(
                    new Track() {
                        Number = int.Parse(trackMatch.Groups[1].Value),
                        Format = trackMatch.Groups[2].Value,
                        Language = trackMatch.Groups[3].Value
                    }
                );

            return tracks;
        }

        public static void WriteTracks(string workingDirectory, string arguments) {
            var process = new Process { StartInfo = startInfo };
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
                Trace.Write('\r' + new string(' ', Trace.IndentSize));
                Trace.Write((isAnalyzing ? "Analyzing" : "Processing") + ": " +
                    line.Substring(line.IndexOf(' ') + 1));
            }

            Trace.WriteLine(string.Empty);
            Trace.Unindent();

            process.WaitForExit();
        }
    }
}
