using System;
using System.Xml.Linq;

namespace Xin1Generator {
    class ChaptersGenerator {
        public XDocument document { get; private set; }
        private XElement root, currentEdition;
        private Random random = new Random();
        private bool hideChapters;

        public ChaptersGenerator(bool hideChapters) {
            document =
                new XDocument(
                    new XDocumentType("Chapters", null, "matroskachapters.dtd", null),
                    root = new XElement("Chapters"));

            this.hideChapters = hideChapters;
        }

        public int CreateEdition() {
            int editionUID = random.Next();

            root.Add(currentEdition =
                new XElement("EditionEntry",
                    new XElement("EditionUID", editionUID),
                    new XElement("EditionFlagHidden", 0),
                    new XElement("EditionFlagDefault", root.HasElements ? 0 : 1),
                    new XElement("EditionFlagOrdered", 1)));

            return editionUID;
        }

        public void CreateChapter(TimeSpan start, TimeSpan end) {
            currentEdition.Add(
                new XElement("ChapterAtom",
                    new XElement("ChapterUID", random.Next()),
                    new XElement("ChapterTimeStart", start.ToString()),
                    new XElement("ChapterTimeEnd", end.ToString()),
                    new XElement("ChapterFlagHidden", hideChapters ? 1 : 0),
                    new XElement("ChapterFlagEnabled", 1)));
        }
    }
}
