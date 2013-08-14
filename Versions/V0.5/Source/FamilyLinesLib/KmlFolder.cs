/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using FamilyLinesLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Represent a base Kml Folder
    /// </summary>
    [XmlRoot("Folder")]
    public abstract class KmlFolder : IXmlSerializable
    {
        #region Properties

        public string Name { get; set; }

        public string Open { get; set; }

        /// <summary>
        /// Total number of places contained in this folder.
        /// </summary>
        public int PlacesCount { get; set; }

        #endregion

        #region Implmentation of IXmlSerializable

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
        }

        public abstract void WriteXml(XmlWriter writer);

        #endregion
    }

    /// <summary>
    /// Represent a Kml Folder with generic children.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [XmlRoot("Folder")]
    public abstract class KmlFolder<T> : KmlFolder where T : IXmlSerializable
    {
        #region Children

        /// <summary>
        /// Children type, either "Folder" or "Placemark"
        /// </summary>
        protected string ChildrenType { get; set; }

        /// <summary>
        /// Children
        /// </summary>
        public T[] Children { get; set; }

        #endregion

        #region Override of WriteXml

        public override void WriteXml(XmlWriter writer)
        {
            writer.WriteElementString("name", Name);
            if (!string.IsNullOrEmpty(Open))
                writer.WriteElementString("open", Open);
            if (Children != null)
            {
                foreach (var child in Children)
                {
                    writer.WriteStartElement(ChildrenType);
                    child.WriteXml(writer);
                    writer.WriteEndElement();
                    //Accumulate number of places.
                    PlacesCount += CalculateCount(child);
                }
            }
        }

        #endregion

        /// <summary>
        /// Method to calculate places.
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected abstract int CalculateCount(T child);
    }

    /// <summary>
    /// Represent a Kml Folder of Placemarks
    /// </summary>
    [XmlRoot("Folder")]
    public class FolderOfPlaces : KmlFolder<KmlPlaceMark>
    {
        #region Constructors

        public FolderOfPlaces()
        {
            ChildrenType = "Placemark";
        }

        public FolderOfPlaces(string name)
            : this()
        {
            Name = name;
            Open = "0";
        }

        #endregion

        /// <summary>
        /// Each place
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected override int CalculateCount(KmlPlaceMark child)
        {
            return 1;
        }
        
    }

    /// <summary>
    /// Represent a Kml Folder of folders.
    /// </summary>
    [XmlRoot("Folder")]
    public class FolderOfFolder : KmlFolder<KmlFolder<KmlPlaceMark>>
    {
        #region Constructors

        public FolderOfFolder()
        {
            ChildrenType = "Folder";
        }

        public FolderOfFolder(string name)
            : this()
        {
            Name = name;
            Open = "0";
        }

        #endregion

        /// <summary>
        /// Each folder's places count.
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected override int CalculateCount(KmlFolder<KmlPlaceMark> child)
        {
            return child.PlacesCount;
        }
    }
}
