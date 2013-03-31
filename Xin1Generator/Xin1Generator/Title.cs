using System;
using System.Collections.Generic;

namespace Xin1Generator {
    public class Title {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Length { get; set; }
        public double FrameRate { get; set; }
        public List<string> Files { get; set; }
        public List<TimeSpan> Chapters { get; set; }

        public Title() {
            Files = new List<string>();
            Chapters = new List<TimeSpan>();
        }

        public Title(Title title) {
            Number = title.Number;
            Name = title.Name;
            Length = title.Length;
            FrameRate = title.FrameRate;
            Files = title.Files;
            Chapters = title.Chapters;
        }
    }
}
