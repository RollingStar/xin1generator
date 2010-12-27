using System;
using System.Collections.Generic;
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

            IDictionary<string, Title> allTitles = Eac3toWrapper.GetTitles(p.InPath);
            int offset = frames[0];

            foreach (string titleNumber in p.TitleNumbers)
                if (!allTitles.ContainsKey(titleNumber))
                    throw new InvalidOperationException("Could not find title " + titleNumber);

            foreach (string titleNumber in p.TitleNumbers) {
                Title title = allTitles[titleNumber];
                titles.Add(title);

                foreach (string file in title.files) {
                    if (files.Contains(file))
                        continue;

                    if (!File.Exists(file))
                        throw new FileNotFoundException("Could not find file " + file);

                    files.Add(file);
                    frames.Add(offset += XportWrapper.GetFrameCount(p.OutPath, file));
                }
            }
        }

        public void GenerateAll(string chaptersName, string tagsName,
                string qpfileName, string demuxName) {
            GenerateChaptersAndTags(chaptersName, tagsName);
            GenerateQpfile(qpfileName);
            GenerateTracksOrCommand(demuxName);
        }

        public void GenerateChaptersAndTags(string chaptersName, string tagsName) {
            Trace.WriteLine("Generating chapters and tags...");

            var chaptersGenerator = new ChaptersGenerator(p.HideChapters);
            var tagsGenerator = new TagsGenerator();

            for (int i = 0; i < titles.Count; i++) {
                Title title = titles[i];
                int editionUID = chaptersGenerator.CreateEdition();

                foreach (string file in title.files) {
                    int idx = files.IndexOf(file);
                    chaptersGenerator.CreateChapter(
                        TimeSpan.FromSeconds(frames[idx] / title.frameRate),
                        TimeSpan.FromSeconds(frames[idx + 1] / title.frameRate));
                }

                tagsGenerator.CreateTag(editionUID,
                    p.TitleNames.Count > i ? p.TitleNames[i] : "Edition " + (i + 1));
            }

            chaptersGenerator.document.Save(Path.Combine(p.OutPath, chaptersName));
            tagsGenerator.document.Save(Path.Combine(p.OutPath, tagsName));
        }

        public void GenerateQpfile(string qpfileName) {
            Trace.WriteLine("Generating qpfile...");

            using (var sw = new StreamWriter(Path.Combine(p.OutPath, qpfileName)))
                for (int i = 1; i < files.Count; i++) // We don't need the first and last frame
                    sw.WriteLine(frames[i] + " I"); // Format: <frame> <type>
        }

        public void GenerateTracksOrCommand(string demuxName) {
            Trace.WriteLine("Generating " + (p.DemuxTracks ? "tracks" : "command") + "...");

            string arguments = "\"" + string.Join("\"+\"", files.ToArray()) + "\" -demux";

            if (p.DemuxTracks)
                Eac3toWrapper.WriteTracks(p.OutPath, arguments);
            else
                using (var sw = new StreamWriter(Path.Combine(p.OutPath, demuxName)))
                    sw.Write(Eac3toWrapper.processFileName + " " + arguments);
        }
    }
}
