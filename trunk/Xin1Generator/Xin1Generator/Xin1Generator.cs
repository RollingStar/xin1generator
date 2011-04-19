using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace Xin1Generator {
    public class Xin1Generator {
        private Parameters p;
        private List<Title> titles = new List<Title>();
        private List<string> files = new List<string>();
        private List<int> frames = new List<int>() { 0 };

        public Xin1Generator(Parameters p) {
            this.p = p;
        }

        public void ExtractAll() {
            ExtractInfo();

            if (p.PreserveChapters)
                ExtractChapters();
        }

        public void ExtractInfo() {
            Trace.WriteLine("Extracting info...");

            List<Title> availableTitles = Eac3toWrapper.GetTitles(p.InputPath);
            int offset = frames[0];

            foreach (Title selectedTitle in p.Titles) {
                Title title = availableTitles.Find(x => x.Number == selectedTitle.Number);

                if (title == null)
                    throw new InvalidOperationException(
                        "Could not find title " + selectedTitle.Number);

                titles.Add(title);

                foreach (string file in title.Files) {
                    if (files.Contains(file))
                        continue;

                    if (!File.Exists(file))
                        throw new FileNotFoundException("Could not find file " + file);

                    files.Add(file);
                }
            }

            for (int i = 0; i < files.Count; i++) {
                Trace.Indent();
                Trace.WriteLine(
                    "[" + (i + 1) + "/" + files.Count + "] " + Path.GetFileName(files[i]));
                Trace.Unindent();

                frames.Add(offset += titles[0].IsBluray ?
                    XportWrapper.GetFrameCount(files[i]) :
                    Eac3toWrapper.GetFrameCount(files[i]));
            }
        }

        public void ExtractChapters() {
            Trace.WriteLine("Extracting chapters...");

            foreach (Title title in titles) {
                List<Track> tracks = Eac3toWrapper.GetTracks(p.InputPath, title.Number);
                Track chaptersTrack = tracks.Find(x => x.Format.Contains("Chapters"));

                if (chaptersTrack == null)
                    continue;

                string tempFile = Path.GetTempFileName();
                Eac3toWrapper.WriteTracks(p.InputPath,
                    title.Number + ") " + chaptersTrack.Number + ":\"" + tempFile + "\"");

                using (var sr = new StreamReader(tempFile)) {
                    string line;

                    while ((line = sr.ReadLine()) != null) {
                        Match match = Regex.Match(line, @"CHAPTER\d{2}=(.+)");

                        if (match.Success)
                            title.Chapters.Add(TimeSpan.Parse(match.Groups[1].Value));
                    }
                }

                File.Delete(tempFile);
            }
        }

        public void GenerateAll() {
            GenerateChaptersAndTags();
            GenerateQpfile();
            GenerateTracksOrCommand();
        }

        public void GenerateChaptersAndTags() {
            Trace.WriteLine("Generating chapters and tags...");

            var chaptersGenerator = new ChaptersGenerator();
            var tagsGenerator = new TagsGenerator();

            for (int i = 0; i < titles.Count; i++) {
                Title title = titles[i];
                var virtualOffset = new TimeSpan(0);
                bool hideNext = !p.PreserveChapters;
                int editionUID = chaptersGenerator.CreateEdition();

                foreach (string file in title.Files) {
                    int idx = files.IndexOf(file);
                    TimeSpan start = TimeSpan.FromSeconds(frames[idx] / title.FrameRate);
                    TimeSpan end = TimeSpan.FromSeconds(frames[idx + 1] / title.FrameRate);
                    TimeSpan nextStart = start;

                    foreach (TimeSpan virtualStart in title.Chapters.FindAll(x =>
                            x > virtualOffset && x <= virtualOffset + end - start)) {
                        chaptersGenerator.CreateChapter(nextStart,
                            nextStart = start + virtualStart - virtualOffset, hideNext);
                        hideNext = false;
                    }

                    virtualOffset += end - start;

                    if (nextStart == end)
                        continue;

                    chaptersGenerator.CreateChapter(nextStart, end, hideNext);
                    hideNext = true;
                }

                tagsGenerator.CreateTag(editionUID, p.Titles[i].Name);
            }

            chaptersGenerator.document.Save(Path.Combine(p.OutputPath, "chapters.xml"));
            tagsGenerator.document.Save(Path.Combine(p.OutputPath, "tags.xml"));
        }

        public void GenerateQpfile() {
            Trace.WriteLine("Generating qpfile...");

            using (var sw = new StreamWriter(Path.Combine(p.OutputPath, "qpfile.txt")))
                for (int i = 1; i < files.Count; i++) // We don't need the first and last frame
                    sw.WriteLine(frames[i] + " I"); // Format: <frame> <type>
        }

        public void GenerateTracksOrCommand() {
            Trace.WriteLine("Generating " + (p.ExtractTracks ? "tracks" : "command") + "...");

            string arguments = "\"" + string.Join("\"+\"", files.ToArray()) + "\"";

            if (p.Tracks.Count > 0)
                foreach (Track track in p.Tracks)
                    arguments +=
                        " " + track.Number + ":track" + track.Number + "." + track.Extension;
            else
                arguments += " -demux";

            if (p.ExtractTracks)
                Eac3toWrapper.WriteTracks(p.OutputPath, arguments);
            else
                using (var sw = new StreamWriter(Path.Combine(p.OutputPath, "extract.cmd")))
                    sw.Write(Eac3toWrapper.processFileName + " " + arguments);
        }

        public static void CheckDependencies() {
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
        }
    }
}
