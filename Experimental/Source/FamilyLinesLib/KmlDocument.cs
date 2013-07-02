using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Represent a Kml Document
    /// </summary>
    [XmlRoot("Document")]
    public class KmlDocument:IXmlSerializable
    {
        #region Properties

        [XmlElement("name")]
        public string Name { get; set; }

        /// <summary>
        /// List of XElement representing styles.
        /// </summary>
        public List<XElement> Styles { get; set; }

        /// <summary>
        /// Each Document has a main Folder.
        /// </summary>
        public KmlFolder MainFolder { get; set; }

        #endregion

        #region Constructors

        public KmlDocument()
        {
            Styles = new List<XElement>();
            Styles.AddRange(KmlStyleFactory.CreateStylesForMale());
            Styles.AddRange(KmlStyleFactory.CreateStylesForFemale());
        }

        public KmlDocument(string name)
            : this()
        {
            Name = name;
        }

        #endregion

        #region Implementation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            //Write name.
            writer.WriteElementString("name", Name);
            //Write style.
            foreach (var style in Styles)
                writer.WriteRaw(style.ToString());
            //Write main folder.
            var xmlSerializer = new XmlSerializer(typeof(KmlFolder));
            xmlSerializer.Serialize(writer, MainFolder);
        }

        #endregion
    }

    /// <summary>
    /// Represent a Kml
    /// </summary>
    [XmlRoot("kml")]
    public class Kml:IXmlSerializable
    {
        #region Namespaces

        private const string Namespace = "http://www.opengis.net/kml/2.2";
        private const string GxNamespace = "http://www.google.com/kml/ext/2.2";
        private const string KmlNamespace = "http://www.opengis.net/kml/2.2";
        private const string AtomNamespace = "http://www.w3.org/2005/Atom";

        #endregion

        #region Properties

        /// <summary>
        /// Each Kml has a Document.
        /// </summary>
        public KmlDocument Document { get; set; }

        /// <summary>
        /// Total places exported after serialization.
        /// </summary>
        public int TotalPlacesExported
        {
            get
            {
                return Document.MainFolder == null ? 0 : Document.MainFolder.PlacesCount;
            }
        }

        #endregion

        #region Constructors

        public Kml()
        {
        }

        public Kml(string name)
        {
            Document = new KmlDocument(name);
        }

        #endregion

        #region Implementation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            //Write namespaces.
            writer.WriteAttributeString("xmlns", Namespace);
            writer.WriteAttributeString("xmlns:gx", GxNamespace);
            writer.WriteAttributeString("xmlns:kml", KmlNamespace);
            writer.WriteAttributeString("xmlns:atom", AtomNamespace);

            //Write document.
            var serializer = new XmlSerializer(typeof(KmlDocument));
            serializer.Serialize(writer, Document);
        }

        #endregion

        #region Creation

        public static Kml Create(string name, ExportType type, ExportOptions options, IEnumerable<Person> people)
        {
            var result = new Kml(name);
            var mainFolder = KmlFolderFactory.CreateFolder(type, options, people);
            result.Document.MainFolder = mainFolder;
            return result;
        }

        #endregion
        
    }
}
