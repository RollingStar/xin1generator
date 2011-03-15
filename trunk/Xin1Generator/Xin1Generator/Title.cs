using System.Collections.Generic;

namespace Xin1Generator {
    public class Title {
        public bool IsBluray { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Length { get; set; }
        public double FrameRate { get; set; }
        public List<string> Files { get; set; }

        public Title() {
            Files = new List<string>();
        }

        public Title(Title title) {
            IsBluray = title.IsBluray;
            Number = title.Number;
            Name = title.Name;
            Length = title.Length;
            FrameRate = title.FrameRate;
            Files = title.Files;
        }
    }
}
