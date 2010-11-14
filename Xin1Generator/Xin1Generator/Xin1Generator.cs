using System;
using System.Collections.Generic;
using System.IO;

namespace Xin1Generator {
    class Xin1Generator {
        private Parameters p;

        private List<Title> titles = new List<Title>();
        private List<string> files = new List<string>();
        private List<int> frames = new List<int>() { 0 };

        public Xin1Generator(Parameters p) {
            this.p = p;
        }

        public void ExtractInfo() {
            Console.WriteLine("Extracting info...");

            IDictionary<string, Title> allTitles = Eac3toWrapper.GetTitles(p.inPath);
            int offset = frames[0];

            foreach (string titleNumber in p.titleNumbers) {
                if (!allTitles.ContainsKey(titleNumber))
                    throw new InvalidOperationException("Could not find title " + titleNumber);

                Title title = allTitles[titleNumber];
                titles.Add(title);

                foreach (string file in title.files) {
                    if (files.Contains(file))
                        continue;

                    if (!File.Exists(file))
                        throw new FileNotFoundException("Could not find file " + file);

                    files.Add(file);
                    frames.Add(offset += XportWrapper.GetFrameCount(p.outPath, file));
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
            Console.WriteLine("Generating chapters and tags...");

            ChaptersGenerator chaptersGenerator = new ChaptersGenerator(p.hideChapters);
            TagsGenerator tagsGenerator = new TagsGenerator();

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
                    p.titleNames.Length > i ? p.titleNames[i] : "Edition " + (i + 1));
            }

            chaptersGenerator.document.Save(Path.Combine(p.outPath, chaptersName));
            tagsGenerator.document.Save(Path.Combine(p.outPath, tagsName));
        }

        public void GenerateQpfile(string qpfileName) {
            Console.WriteLine("Generating qpfile...");

            using (StreamWriter sw = new StreamWriter(Path.Combine(p.outPath, qpfileName)))
                for (int i = 1; i < files.Count; i++) // We don't need the first and last frame
                    sw.WriteLine(frames[i] + " I"); // Format: <frame> <type>
        }

        public void GenerateTracksOrCommand(string demuxName) {
            Console.WriteLine("Generating " + (p.demuxTracks ? "tracks" : "command") + "...");

            string arguments = "\"" + string.Join("\"+\"", files.ToArray()) + "\" -demux";

            if (p.demuxTracks)
                Eac3toWrapper.WriteTracks(p.outPath, arguments);
            else
                using (StreamWriter sw = new StreamWriter(Path.Combine(p.outPath, demuxName)))
                    sw.Write(Eac3toWrapper.processFileName + " " + arguments);
        }
    }
}
