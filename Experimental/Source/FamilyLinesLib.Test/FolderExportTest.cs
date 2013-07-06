/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using KBS.FamilyLinesLib;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FamilyLinesLib.Test
{
    [TestClass]
    public class FolderExportTest
    {
        [TestMethod]
        public void FolderOfPlacesExportTest()
        {
            var place=new KmlPlaceMark("Bill Gates", "WA, USA", "Microsoft", GEDCOM.Net.Gender.Male);
            var sut = new FolderOfPlaces("AFolder") { Children = new[] { place } };

            var expected = new XElement("Folder",
                new XElement("name", "AFolder"),
                new XElement("open", "0"),
                XmlSerializerHelper.ToXElement(place));
            var actual=XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        [TestMethod]
        public void FolderOfFolderExportTest()
        {
            var place = new KmlPlaceMark("Bill Gates", "WA, USA", "Microsoft", GEDCOM.Net.Gender.Male);
            var folder = new FolderOfPlaces("AFolder") { Children = new[] { place } };
            var sut = new FolderOfFolder("Whole") { Children = new[] { folder } };

            var expected = new XElement("Folder",
                new XElement("name", "Whole"),
                new XElement("open", "0"),
                XmlSerializerHelper.ToXElement(folder));
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        [TestMethod]
        public void FolderCreaterWithPeopleTest()
        {
            var person = MakeBasicBillG();
            person.CremationDate = new DateTime(2013, 1, 1);
            person.CremationPlace = "New York";
            var people = new List<Person> {person};

            var sut = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.Places, ExportOptions.Births | ExportOptions.Cremations, people);
            var expected = new XElement("Folder",
                new XElement("name", "People"),
                new XElement("open", "0"),
                new XElement("Folder",
                    new XElement("name", "Births"),
                    new XElement("open", "0"),
                    new XElement("Placemark",
                        new XElement("name", "Bill Gates"),
                        new XElement("address", "Seattle"),
                        new XElement("description", "Seattle"),
                        new XElement("styleUrl", "#msn_man"))),
                new XElement("Folder",
                    new XElement("name", "Cremations"),
                    new XElement("open", "0"),
                    new XElement("Placemark",
                        new XElement("name", "Bill Gates"),
                        new XElement("address", "New York"),
                        new XElement("description", "New York"),
                        new XElement("styleUrl", "#msn_man"))));
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        private Person MakeBasicBillG()
        {
            return new Person
                       {
                           FirstName = "Bill",
                           LastName = "Gates",
                           BirthPlace = "Seattle",
                           BirthDate = new DateTime(1977, 1, 1),
                           Gender = GEDCOM.Net.Gender.Male
                       };
        }

        [TestMethod]
        public void FolderCreaterWithPeopleTimeTest()
        {
            var people = new List<Person>{ MakeBasicBillG() };

            var sut = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.PlacesWithTimes, ExportOptions.Births | ExportOptions.Cremations, people);
            var expected = new XElement("Folder",
                new XElement("name", "Events"),
                new XElement("open", "0"),
                new XElement("Folder",
                    new XElement("name", "Births"),
                    new XElement("open", "0"),
                    new XElement("Placemark",
                        new XElement("name", "Bill Gates"),
                        new XElement("address", "Seattle"),
                        new XElement("description", "Seattle"),
                        new XElement("TimeStamp",
                            new XElement("when", "1977")),
                        new XElement("styleUrl", "#msn_man"))),
                 new XElement("Folder",
                     new XElement("name", "Cremations"),
                     new XElement("open", "0")));
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        [TestMethod]
        public void FolderCreaterWithPeopleTimeMarriageTest()
        {
            var person = MakeBasicBillG();
            person.Relationships.Add(new SpouseRelationship(person, SpouseModifier.Current) { MarriageDate = new DateTime(1990, 1, 1), MarriagePlace = "Los Angeles" });

            var people = new List<Person> { person };

            var sut = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.PlacesWithTimes, ExportOptions.Marriages, people);
            var expected = new XElement("Folder",
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
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        [TestMethod]
        public void FolderCreaterWithPeopleTimeDivorceWithNoDivorceTest()
        {
            var people = new List<Person> { MakeBasicBillG() };

            var sut = KmlFolderFactory.CreateFolderWithTimeStamp(ExportType.PlacesWithTimes, ExportOptions.Divorces, people);
            var expected = new XElement("Folder",
                new XElement("name", "Events"),
                new XElement("open", "0"),
                new XElement("Folder",
                    new XElement("name", "Divorces"),
                    new XElement("open", "0")));
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }

        [TestMethod]
        public void FolderCreaterWithPeopleLifeTest()
        {
            var people = new List<Person> { MakeBasicBillG() };

            var sut = KmlFolderFactory.CreateFolder(ExportType.Lifetimes, ExportOptions.Births, people);
            var expected = new XElement("Folder",
                new XElement("name", "People"),
                new XElement("open", "0"),
                new XElement("Placemark",
                    new XElement("name", "Bill Gates"),
                    new XElement("address", "Seattle"),
                    new XElement("description", "Seattle"),
                    new XElement("TimeSpan",
                        new XElement("begin", "1977"),
                        new XElement("end", "-")),
                    new XElement("styleUrl", "#msn_man")));
            var actual=XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(actual.ToString(), expected.ToString());
        }
    }
}
