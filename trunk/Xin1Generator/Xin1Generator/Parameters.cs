using System.Collections.Generic;

namespace Xin1Generator {
    public class Parameters {
        public readonly List<Title> Titles = new List<Title>();
        public readonly List<Track> Tracks = new List<Track>();
        public string InputPath, OutputPath;
        public bool ExtractTracks;
        public bool PreserveChapters;
    }
}
