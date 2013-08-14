/*
 * Family.Show derived code provided under MS-PL license.
 */
/*
* Exports time encoded place information to a kml file.
* 
* Three sections are exported: Events and People
* 1. Events are births, marriages and deaths and the year that the event occurs is included.
* 2. People are exported with a timespan from date of birth to date of death.
* 3. All places with no time information.
* 
* The format is based on the open kml standard for map information.
* The recommended software for reading the file is Google Earth as
* this program will search for coordinate information of places 
* specified in the file. Other similar services such as Bing maps 
* require coordinates to be specifed in the file.  Google Earth can also
* read the time information and allows the user to specify a time period to
* display e.g. 1900-1950.
* 
* Places export supports restrictions and quick filter for living people.
*/

using System.IO;
using GEDCOM.Net;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.Linq;
using System.Xml.Linq;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Export options.
    /// </summary>
    [Flags]
    public enum ExportOptions
    {
        None = 0,
        HideLivingPeople = 1,
        Births = 2,
        Deaths = 4,
        Cremations = 8,
        Burials = 16,
        Marriages = 32,
        Divorces = 64
    }

    /// <summary>
    /// Export types.
    /// </summary>
    public enum ExportType
    {
        Places,
        PlacesWithTimes,
        Lifetimes
    }

    public class PlacesExport
    {

        #region export methods

        public static string[] ExportPlaces(IEnumerable<Person> people, string filename, ExportType type, ExportOptions options)
        {
            //Get export name.
            var name = Path.GetFileNameWithoutExtension(filename);

            //Filter people.
            var filteredPeople = people.Where(item => item.Restriction != Restriction.Private && (!options.HasFlag(ExportOptions.HideLivingPeople) || !item.IsLiving));

            //Create Kml.
            var kml = Kml.Create(name, type, options, filteredPeople);

            //Write to file.
            using (var writer = new XmlTextWriter(filename, Encoding.UTF8))
            {
                var serializer = new XmlSerializer(typeof(Kml));
                serializer.Serialize(writer, kml);
            }

            //Get number of places and return summary.
            string[] summary = new string[2];

            summary[0] = kml.TotalPlacesExported.ToString() + " " + KBS.FamilyLinesLib.Properties.Resources.PlacesExported;
            summary[1] = filename.ToString();

            if (kml.TotalPlacesExported == 0)
            {
                File.Delete(filename);
                summary[0] = KBS.FamilyLinesLib.Properties.Resources.NoPlaces;
                summary[1] = "No file";
            }

            return summary;
        }

        #endregion

    }
}

