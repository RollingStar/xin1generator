﻿using System.Xml.Linq;

namespace Xin1Generator {
    class TagsGenerator {
        public XDocument document { get; private set; }

        public TagsGenerator() {
            document =
                new XDocument(
                    new XDocumentType("Tags", null, "matroskatags.dtd", null),
                    new XElement("Tags"));
        }

        public void CreateTag(int editionUID, string title) {
            document.Root.Add(
                new XElement("Tag",
                    new XElement("Targets",
                        new XElement("EditionUID", editionUID)),
                    new XElement("Simple",
                        new XElement("Name", "TITLE"),
                        new XElement("String", title))));
        }
    }
}
