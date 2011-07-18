using System;
using System.Xml.Linq;

namespace Xin1Generator {
    class ChaptersGenerator {
        private const string timeFormat = @"hh\:mm\:ss\.fff";

        public XDocument document { get; private set; }
        private XElement currentEdition;
        private Random random = new Random();

        public ChaptersGenerator() {
            document =
                new XDocument(
                    new XDocumentType("Chapters", null, "matroskachapters.dtd", null),
                    new XElement("Chapters"));
        }

        public int CreateEdition() {
            int editionUID = random.Next();

            document.Root.Add(currentEdition =
                new XElement("EditionEntry",
                    new XElement("EditionUID", editionUID),
                    new XElement("EditionFlagHidden", 0),
                    new XElement("EditionFlagDefault", document.Root.HasElements ? 0 : 1),
                    new XElement("EditionFlagOrdered", 1)));

            return editionUID;
        }

        public void CreateChapter(TimeSpan start, TimeSpan end, bool hideChapter) {
            XElement chapter;

            currentEdition.Add(chapter =
                new XElement("ChapterAtom",
                    new XElement("ChapterUID", random.Next()),
                    new XElement("ChapterTimeStart", start.ToString(timeFormat)),
                    new XElement("ChapterTimeEnd", end.ToString(timeFormat)),
                    new XElement("ChapterFlagHidden", hideChapter ? 1 : 0),
                    new XElement("ChapterFlagEnabled", 1)));

            if (!hideChapter)
                chapter.Add(
                    new XElement("ChapterDisplay",
                        new XElement("ChapterString", string.Empty),
                        new XElement("ChapterLanguage", "eng")));
        }
    }
}
