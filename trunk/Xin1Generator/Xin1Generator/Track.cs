namespace Xin1Generator {
    public class Track {
        public bool IsUsed { get; set; }
        public int Number { get; set; }
        public string Format { get; set; }
        public string Language { get; set; }
        public string Extension { get; set; }

        public Track() {
            Extension = "*";
        }
    }
}
