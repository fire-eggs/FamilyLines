using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FamilyLinesLib;
using GEDCOM.Net;
using System.Xml.Linq;

namespace FamilyLinesLib.Test
{
    [TestClass]
    public class PlacesExportTest
    {
        private TestContext _testContext;

        public TestContext TestContext
        {
            get { return _testContext; }
            set { _testContext = value; }
        }

        [TestMethod]
        [DataSource("PerosonMarkExcelDataSource")]
        public void ExportPeople()
        {
            var name = TestContext.DataRow["Name"].ToString();
            if (name == "null") name = null;
            var address = TestContext.DataRow["Address"].ToString();
            if (address == "null") address = null;
            var description=TestContext.DataRow["Description"].ToString();
            if (description == "null") description = null;
            Gender gender;
            Assert.IsTrue(Gender.TryParse(TestContext.DataRow["Gender"].ToString(), out gender), "Data is incorrect");

            var sut = new KmlPlaceMark(name, address, description, gender);

            var expected = XElement.Parse(TestContext.DataRow["Xml"].ToString());
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        [TestMethod]
        [DataSource("PerosonMarkWithTimeStampExcelDataSource")]
        public void ExportPeopleWithTimeStampTest()
        {
            var name = TestContext.DataRow["Name"].ToString();
            if (name == "null") name = null;
            var address = TestContext.DataRow["Address"].ToString();
            if (address == "null") address = null;
            var description = TestContext.DataRow["Description"].ToString();
            if (description == "null") description = null;
            var when = TestContext.DataRow["When"].ToString();
            if (when == "null") when = null;
            Gender gender;
            Assert.IsTrue(Gender.TryParse(TestContext.DataRow["Gender"].ToString(), out gender), "Data is incorrect");

            var sut = new PersonPlaceMarkWithTimeStamp(name, address, description, gender) { TimeStamp = new KmlTimeStamp(when) };

            var expected = XElement.Parse(TestContext.DataRow["Xml"].ToString());
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }

        [TestMethod]
        [DataSource("PerosonMarkWithTimeSpanExcelDataSource")]
        public void ExportPeopleWithTimeSpanTest()
        {
            var name = TestContext.DataRow["Name"].ToString();
            if (name == "null") name = null;
            var address = TestContext.DataRow["Address"].ToString();
            if (address == "null") address = null;
            var description = TestContext.DataRow["Description"].ToString();
            if (description == "null") description = null;
            var begin = TestContext.DataRow["Begin"].ToString();
            if (begin == "null") begin = null;
            var end = TestContext.DataRow["End"].ToString();
            if (end == "null") end = null;
            Gender gender;
            Assert.IsTrue(Gender.TryParse(TestContext.DataRow["Gender"].ToString(), out gender), "Data is incorrect");

            var sut = new PersonPlaceMarkWithTimeSpan(name, address, description, gender) { TimeSpan = new KmlTimeSpan(begin, end ) };

            var expected = XElement.Parse(TestContext.DataRow["Xml"].ToString());
            var actual = XmlSerializerHelper.ToXElement(sut);
            Assert.AreEqual(expected.ToString(), actual.ToString());
        }
    }
}
