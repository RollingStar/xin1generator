namespace Xin1GeneratorGUI {
    public class AvailableTitle {
        public int Number { get; set; }
        public string Length { get; set; }

        public AvailableTitle(int Number, string Length) {
            this.Number = Number;
            this.Length = Length;
        }
    }

    public class SelectedTitle {
        public int Number { get; set; }
        public string Name { get; set; }

        public SelectedTitle(int Number) {
            this.Number = Number;
        }
    }
}
