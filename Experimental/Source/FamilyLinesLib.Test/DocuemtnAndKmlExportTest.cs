using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using KBS.FamilyLinesLib;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FamilyLinesLib.Test
{
    [TestClass]
    public class DocuemtnAndKmlExportTest
    {
        [TestMethod]
        public void DocumentExportTest()
        {
            var person = new Person { FirstName = "Bill", LastName = "Gates", BirthPlace = "Seatle", BirthDate = new DateTime(1977, 1, 1), Gender = GEDCOM.Net.Gender.Male };
            person.Relationships.Add(new SpouseRelationship(person, SpouseModifier.Current) { MarriageDate = new DateTime(1990, 1, 1), MarriagePlace = "Los Angeles" });

            var people = new List<Person> { person };

            var folder = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.PlacesWithTimes, ExportOptions.Marriages, people);
            var sut = new KmlDocument("Output") { MainFolder = folder };
            var expectedFolder = new XElement("Folder",
                new XElement("name", "Events"),
                new XElement("open", "0"),
                new XElement("Folder",
                    new XElement("name", "Marriages"),
                    new XElement("open", "0"),
                    new XElement("Placemark",
                        new XElement("name", "Bill Gates"),
                        new XElement("address", "Los Angeles"),
                        new XElement("description", "Los Angeles"),
                        new XElement("TimeStamp",
                            new XElement("when", "1990")),
                        new XElement("styleUrl", "#msn_man"))));
            var serializedElement = XmlSerializerHelper.ToXElement(sut);
            var actualFolder = serializedElement.Element("Folder");
            var actualName = serializedElement.Element("name").Value.ToString();
            Assert.AreEqual(expectedFolder.ToString(), actualFolder.ToString());
            Assert.AreEqual("Output", actualName);
        }

        [TestMethod]
        public void KmlExportTest()
        {
            var person = new Person { FirstName = "Bill", LastName = "Gates", BirthPlace = "Seatle", BirthDate = new DateTime(1977, 1, 1), Gender = GEDCOM.Net.Gender.Male };
            person.Relationships.Add(new SpouseRelationship(person, SpouseModifier.Current) { MarriageDate = new DateTime(1990, 1, 1), MarriagePlace = "Los Angeles" });

            var people = new List<Person> { person };

            var folder = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.PlacesWithTimes, ExportOptions.Marriages, people);
            var sut = Kml.Create("NewKml", ExportType.PlacesWithTimes, ExportOptions.Marriages, people);
            var expectedgxNamespace = "http://www.google.com/kml/ext/2.2";
            var serializedElement = XmlSerializerHelper.ToXElement(sut);
            XNamespace ns = "http://www.opengis.net/kml/2.2";
            var actualgxNamespace = serializedElement.GetNamespaceOfPrefix("gx").NamespaceName;
            var actualName = serializedElement.Element(ns + "Document").Element(ns + "name").Value.ToString();
            Assert.AreEqual("NewKml", actualName);
            Assert.AreEqual(expectedgxNamespace, actualgxNamespace);
        }
    }
}
