using System.Collections.Generic;

namespace Xin1Generator {
    public class Parameters {
        public readonly List<int> TitleNumbers = new List<int>();
        public readonly List<string> TitleNames = new List<string>();
        public string InputPath, OutputPath;
        public bool DemuxTracks = false;
        public bool HideChapters = false;
    }
}
