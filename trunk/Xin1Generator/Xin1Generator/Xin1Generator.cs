using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Xin1Generator {
    public class Xin1Generator {
        private Parameters p;
        private List<Title> titles = new List<Title>();
        private List<string> files = new List<string>();
        private List<int> frames = new List<int>() { 0 };

        public Xin1Generator(Parameters p) {
            this.p = p;
        }

        public void ExtractInfo() {
            Trace.WriteLine("Extracting info...");

            List<Title> availableTitles = Eac3toWrapper.GetTitles(p.InputPath);
            int offset = frames[0];

            foreach (Title selectedTitle in p.Titles)
                if (!availableTitles.Exists(x => x.Number == selectedTitle.Number))
                    throw new InvalidOperationException(
                        "Could not find title " + selectedTitle.Number);

            foreach (Title selectedTitle in p.Titles) {
                Title title = availableTitles.Find(x => x.Number == selectedTitle.Number);
                titles.Add(title);

                foreach (string file in title.Files) {
                    if (files.Contains(file))
                        continue;

                    if (!File.Exists(file))
                        throw new FileNotFoundException("Could not find file " + file);

                    files.Add(file);
                    frames.Add(offset += XportWrapper.GetFrameCount(p.OutputPath, file));
                }
            }
        }

        public void GenerateAll() {
            GenerateChaptersAndTags();
            GenerateQpfile();
            GenerateTracksOrCommand();
        }

        public void GenerateChaptersAndTags() {
            Trace.WriteLine("Generating chapters and tags...");

            var chaptersGenerator = new ChaptersGenerator(p.HideChapters);
            var tagsGenerator = new TagsGenerator();

            for (int i = 0; i < titles.Count; i++) {
                Title title = titles[i];
                int editionUID = chaptersGenerator.CreateEdition();

                foreach (string file in title.Files) {
                    int idx = files.IndexOf(file);
                    chaptersGenerator.CreateChapter(
                        TimeSpan.FromSeconds(frames[idx] / title.FrameRate),
                        TimeSpan.FromSeconds(frames[idx + 1] / title.FrameRate));
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
