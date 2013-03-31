using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    public static class Eac3toWrapper {
        public const string processFileName = "eac3to";

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
                process.StartInfo.Arguments = "\"" + path + "\" -check";
                process.StartInfo.WorkingDirectory = string.Empty;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            Match frameCountMatch =
                Regex.Match(output, @"Video track \d+ contains (\d+) frames\.");

            if (!frameCountMatch.Success)
                throw new InvalidOperationException(
                    "Could not determine frame count of " + path);

            return int.Parse(frameCountMatch.Groups[1].Value);
        }

        public static List<Title> GetTitles(string path) {
            string output;

            using (var process = new Process { StartInfo = GetStartInfo() }) {
                process.StartInfo.Arguments = "\"" + path + "\"";
                process.StartInfo.WorkingDirectory = string.Empty;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            var titles = new List<Title>();

            MatchCollection titleMatches = Regex.Matches(output,
                @"(\d+)\) (?:\d+\.mpls.*?, )?(.+?(?:\n[\s\b]*\n|.$))", RegexOptions.Singleline);

            foreach (Match titleMatch in titleMatches) {
                var title = new Title();

                Match lengthMatch = Regex.Match(titleMatch.Groups[2].Value, @"(?:\d+\:){2}\d+");
                Match nameMatch = Regex.Match(titleMatch.Groups[2].Value, @"""(\w+)""");
                Match filesMatch = Regex.Match(titleMatch.Groups[2].Value, @"\S+(\.\w+)");
                Match frameRateMatch =
                    Regex.Match(titleMatch.Groups[2].Value, @"([pi])(\d+)(?: \/(\d+\.\d+))?");

                title.Number = int.Parse(titleMatch.Groups[1].Value);
                title.Name = !nameMatch.Success ? null :
                    Regex.Replace(nameMatch.Groups[1].Value, @"([a-z])([A-Z0-9])", "$1 $2");
                title.Length = lengthMatch.Value;

                if (!frameRateMatch.Success)
                    frameRateMatch = GetFrameRateMatch(path, title.Number);

                title.FrameRate = int.Parse(frameRateMatch.Groups[2].Value) /
                    (frameRateMatch.Groups[3].Success ?
                        double.Parse(frameRateMatch.Groups[3].Value,
                            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) : 1) /
                    (frameRateMatch.Groups[1].Value == "i" ? 2 : 1);

                string ext = filesMatch.Groups[1].Value;
                string files =
                    filesMatch.Value.Replace(ext, string.Empty).Trim(new[] { '[', ']' });

                foreach (string file in files.Split('+'))
                    title.Files.Add(Path.Combine(path, Utilities.IsBluray(path) ?
                        Path.Combine("BDMV", "STREAM", int.Parse(file).ToString("D5")) :
                        Path.Combine("HVDVD_TS", file)) + ext);

                titles.Add(title);
            }

            return titles;
        }

        private static Match GetFrameRateMatch(string path, int titleNumber) {
            string output;

            using (var process = new Process { StartInfo = GetStartInfo() }) {
                process.StartInfo.Arguments = "\"" + path + "\" " + titleNumber + ")";
                process.StartInfo.WorkingDirectory = string.Empty;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            Match frameRateMatch =
                Regex.Match(output, @"([pi])(\d+)(?: \/(\d+\.\d+))?", RegexOptions.Singleline);

            if (!frameRateMatch.Success)
                throw new InvalidOperationException(
                    "Could not determine frame rate of " + path);

            return frameRateMatch;
        }

        public static List<Track> GetTracks(string path) {
            return GetTracks(path, 0);
        }

        public static List<Track> GetTracks(string path, int titleNumber) {
            string output;

            using (var process = new Process { StartInfo = GetStartInfo() }) {
                process.StartInfo.Arguments = "\"" + path + "\"" +
                    (titleNumber > 0 ? " " + titleNumber + ")" : string.Empty);
                process.StartInfo.WorkingDirectory = string.Empty;
                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }

            var tracks = new List<Track>();

            MatchCollection trackMatches =
                Regex.Matches(output, @"(\d+): (.+?), ([A-Z][a-z]+)?");

            foreach (Match trackMatch in trackMatches)
                tracks.Add(new Track() {
                    Number = int.Parse(trackMatch.Groups[1].Value),
                    Format = trackMatch.Groups[2].Value,
                    Language = trackMatch.Groups[3].Value
                });

            return tracks;
        }

        public static void WriteTracks(string workingDirectory, string arguments) {
            using (var process = new Process { StartInfo = GetStartInfo() }) {
                process.StartInfo.Arguments = arguments + " -progressnumbers";
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.Start();

                string line;
                bool isAnalyzing = true;

                Trace.Indent();

                while ((line = process.StandardOutput.ReadLine()) != null) {
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
}
