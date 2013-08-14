/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using GEDCOM.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace FamilyLinesLib
{
    [XmlRoot("TimeStamp")]
    public class KmlTimeStamp : IXmlSerializable
    {
        #region Property

        /// <summary>
        /// Represent a Kml TimeStamp
        /// </summary>
        [XmlElement("when")]
        public string When { get; set; }

        #endregion

        #region Constructors

        public KmlTimeStamp()
        {
        }

        public KmlTimeStamp(string when)
        {
            When = when;
        }

        #endregion

        #region Implementation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public void WriteXml(XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(When))
                writer.WriteElementString("when", When);
        }

        #endregion
    }

    /// <summary>
    /// Represent a Kml TimeSpan
    /// </summary>
    [XmlRoot("TimeSpan")]
    public class KmlTimeSpan : IXmlSerializable
    {
        #region Properties

        [XmlElement("begin")]
        public string Begin { get; set; }

        [XmlElement("end")]
        public string End { get; set; }

        #endregion

        #region Constructors

        public KmlTimeSpan()
        {
        }

        public KmlTimeSpan(string begin, string end)
        {
            Begin = begin;
            End = end;
        }

        #endregion

        #region Implementation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public void WriteXml(XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(Begin))
                writer.WriteElementString("begin", Begin);
            if (!string.IsNullOrEmpty(End))
                writer.WriteElementString("end", End);
        }

        #endregion
    }

    /// <summary>
    /// Represent a base Kml Placemark
    /// </summary>
    [XmlRoot("Placemark")]
    public class KmlPlaceMark : IXmlSerializable
    {
        #region Properties

        [XmlElement("name")]
        public string Name { get; set; }

        [XmlElement("address")]
        public string Address { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("styleUrl")]
        public string Style { get; set; }

        #endregion

        #region Constructors

        public KmlPlaceMark()
        {
        }

        public KmlPlaceMark(string name, string address, string description, Gender gender)
        {
            Name = name;
            Address = address;
            Description = description;
            Style = ExportPlaceHelper.GetStyleFromGender(gender);
        }

        #endregion

        #region Implementation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("name", Name);
            writer.WriteElementString("address", Address);
            writer.WriteElementString("description", Description);
            writer.WriteElementString("styleUrl", Style);
        }

        #endregion
    }

    /// <summary>
    /// Represent a KmlPlacemark with TimeStamp
    /// </summary>
    [XmlRoot("Placemark")]
    public class PersonPlaceMarkWithTimeStamp : KmlPlaceMark
    {
        #region Property

        [XmlElement("TimeStamp")]
        public KmlTimeStamp TimeStamp { get; set; }

        #endregion

        #region Constructors

        public PersonPlaceMarkWithTimeStamp()
        {
        }

        public PersonPlaceMarkWithTimeStamp(string name, string address, string description, Gender gender)
            : base(name, address, description, gender)
        {
        }

        #endregion

        #region Override of WriteXml

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("name", Name);
            writer.WriteElementString("address", Address);
            writer.WriteElementString("description", Description);
            
            if (TimeStamp != null)
            {
                var xmlSerializer = new XmlSerializer(typeof(KmlTimeStamp));
                xmlSerializer.Serialize(writer, TimeStamp);
            }

            writer.WriteElementString("styleUrl", Style);
        }

        #endregion   
    }

    /// <summary>
    /// Represent a KmlPlacemark with TimeSpan
    /// </summary>
    [XmlRoot("Placemark")]
    public class PersonPlaceMarkWithTimeSpan : KmlPlaceMark
    {
        #region Property

        [XmlElement("TimeSpan")]
        public KmlTimeSpan TimeSpan { get; set; }

        #endregion

        #region Constructors

        public PersonPlaceMarkWithTimeSpan()
        {
        }

        public PersonPlaceMarkWithTimeSpan(string name, string address, string description, Gender gender)
            : base(name, address, description, gender)
        {
        }

        #endregion

        #region Override of WriteXml

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("name", Name);
            writer.WriteElementString("address", Address);
            writer.WriteElementString("description", Description);
            if (TimeSpan != null)
            {
                var xmlSerializer = new XmlSerializer(typeof(KmlTimeSpan));
                xmlSerializer.Serialize(writer, TimeSpan);
            }
            writer.WriteElementString("styleUrl", Style);
        }

        #endregion

        
    }

    public static class ExportPlaceHelper
    {
        public static string GetStyleFromGender(Gender gender)
        {
            if (gender == Gender.Male)
                return "#msn_man";
            if (gender == Gender.Female)
                return "#msn_woman";
            throw new ArgumentException("gender", "Not supported");
        }
    }
}
