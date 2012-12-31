using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using SharpKml.Base;
using SharpKml.Dom;

namespace KBS.FamilyLinesLib
{
    public class Experimental
    {

        // common logic for hashing a person event to location
        private static void addHash(Hashtable hash, Person person, string loc)
        {
            loc = loc.Replace("  ", " ").ToLower();

            List<string> people;
            if (!hash.ContainsKey(loc))
            {
                // TODO Use LastName to get only surnames in a location
                people = new List<string> { person.FullName };
                hash.Add(loc, people);
            }
            else
            {
                people = hash[loc] as List<string>;
                // TODO Use LastName to get only surnames in a location
                if (!people.Contains(person.FullName))
                    people.Add(person.FullName);
            }
        }

        // Produce a concordance file for location to people's names
        // TODO: consider a list view allowing navigation to people for correction; options such as skip living, etc; invoke the file
        public static void LocationConcordance(string destFilePath, PeopleCollection people)
        {
            var filen = Path.GetFileNameWithoutExtension(destFilePath) + ".txt";

            var fpath = Path.GetDirectoryName(destFilePath);

            filen = Path.Combine(fpath, filen);

            try
            {
                Hashtable locHash = new Hashtable();
                foreach (var person in people)
                {
                    if (!string.IsNullOrEmpty(person.BirthPlace))
                    {
                        addHash(locHash, person, person.BirthPlace);
                    }
                    if (!string.IsNullOrEmpty(person.DeathPlace))
                    {
                        addHash(locHash, person, person.DeathPlace);
                    }

                }

                var sb = new StringBuilder();

                List<string> locs = (from object loc in locHash.Keys select loc.ToString()).ToList();
                locs.Sort();

                foreach (var entry in locs)
                {
                    sb.Append(entry);
                    sb.Append('|');

                    var locPeople = locHash[entry] as List<string>;
                    locPeople.Sort();
                    foreach (var personName in locPeople)
                    {
                        sb.Append('"');
                        sb.Append(personName);
                        sb.Append("\",");
                    }
                    sb.AppendLine();
                }
                File.WriteAllText(filen, sb.ToString());

            }
            catch (Exception)
            {
                // TODO need a better response
            }
        }

        private static List<DateTime> spanDates = new List<DateTime> { new DateTime(1700,1,1), new DateTime(1750,1,1), new DateTime(1800,1,1), new DateTime(1850,1,1), 
                                                                       new DateTime(1900,1,1), new DateTime(1950,1,1), new DateTime(2200,1,1)};
        private static void AddToSpan(List<Folder> spans, Placemark feat, DateTime val)
        {
            for (int i = 0; i < spans.Count; i++)
            {
                if (val < spanDates[i])
                {
                    spans[i].AddFeature(feat);
                    return;
                }
            }
        }

        private static Hashtable latlongHash;

        // Fetch a lat/long coord pair from Google for an address
        private static Tuple<double, double> GetCoord(string region)
        {
            var res = latlongHash[region];
            if (res != null)
                return res as Tuple<double, double>;

            using (var client = new WebClient())
            {

                string uri = "http://maps.google.com/maps/geo?q='" + region +
                  "'&output=csv&key=ABQIAAAAzr2EBOXUKnm_jVnk0OJI7xSosDVG8KKPE1" +
                  "-m51RBrvYughuyMxQ-i1QfUnH94QxWIa6N4U6MouMmBA";

                string[] geocodeInfo = client.DownloadString(uri).Split(',');

                var res2 = new Tuple<double, double>(Convert.ToDouble(geocodeInfo[2]),
                           Convert.ToDouble(geocodeInfo[3]));
                latlongHash[region] = res2;

                System.Threading.Thread.Sleep(150);

                return res2;
            }

        }

        // Fetch a lat/long coord pair from Google for an address
        private static string GetCoord2(string region)
        {
            try
            {
                using (var client = new WebClient())
                {
                    string uri = "http://maps.google.com/maps/geo?q='" + region +
                      "'&output=csv&key=ABQIAAAAzr2EBOXUKnm_jVnk0OJI7xSosDVG8KKPE1" +
                      "-m51RBrvYughuyMxQ-i1QfUnH94QxWIa6N4U6MouMmBA";

                    string[] geocodeInfo = client.DownloadString(uri).Split(',');

                    return geocodeInfo[2] + geocodeInfo[3];
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static void DumpKMLSpans(string gedcomFilePath, PeopleCollection people)
        {
            latlongHash = new Hashtable();

            const string boyB = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=!|7fffff";
            const string girlB = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=!|ff7fff";

            var balloon = new BalloonStyle { Text = "<![CDATA[$[description]]]>" };

            var boyStyle = new Style();
            boyStyle.Id = "boyIcon";
            boyStyle.Icon = new IconStyle();
            boyStyle.Icon.Icon = new IconStyle.IconLink(new Uri(boyB));
            boyStyle.Icon.Scale = 1.0;
            boyStyle.Balloon = balloon;

            var girlStyle = new Style();
            girlStyle.Id = "girlIcon";
            girlStyle.Icon = new IconStyle();
            girlStyle.Icon.Icon = new IconStyle.IconLink(new Uri(girlB));
            girlStyle.Icon.Scale = 1.0;
            girlStyle.Balloon = balloon;

            var doc = new Document();
            doc.AddStyle(girlStyle);
            doc.AddStyle(boyStyle);

            Folder timespans = new Folder { Name = "Time Spans" };
            Folder spanTo1700 = new Folder { Name = "Before 1700" };
            Folder span1700 = new Folder { Name = "1700 - 1750" };
            Folder span1750 = new Folder { Name = "1750 - 1800" };
            Folder span1800 = new Folder { Name = "1800 - 1850" };
            Folder span1850 = new Folder { Name = "1850 - 1900" };
            Folder span1900 = new Folder { Name = "1900 - 1950" };
            Folder span1950 = new Folder { Name = "1950 on" };
            List<Folder> spans = new List<Folder> { spanTo1700, span1700, span1750, span1800, span1850, span1900, span1950 };
            foreach (var folder in spans)
            {
                timespans.AddFeature(folder);
            }

            foreach (var person in people)
            {
                if (person.BirthDate != null && person.BirthPlace.Length > 0)
                {
                    var feat = makeBirthMarkLL(person);
                    AddToSpan(spans, feat, person.BirthDate.GetValueOrDefault());
                }
            }

            doc.AddFeature(timespans);

            var ser = new Serializer();
            ser.Serialize(doc);

            var filen = Path.GetFileNameWithoutExtension(gedcomFilePath);
            File.WriteAllText(filen + "_spans.kml", ser.Xml);

        }

        // Produce a KML file with placemarks for birth, death and marriage
        public static void DumpKML(string gedcomFilePath, PeopleCollection people)
        {
            try
            {
                var doc = new Document();

                //var girlStyle = new Style();
                //girlStyle.Id = "girlIcon";
                //girlStyle.Icon = new IconStyle();
                //girlStyle.Icon.Color = new Color32(255, 255, 128, 255);
                //girlStyle.Icon.ColorMode = ColorMode.Normal;
                //girlStyle.Icon.Icon = new IconStyle.IconLink(new Uri("http://maps.google.com/mapfiles/kml/pal3/icon21.png"));
                //girlStyle.Icon.Scale = 1.0;

                //var boyStyle = new Style();
                //boyStyle.Id = "boyIcon";
                //boyStyle.Icon = new IconStyle();
                //boyStyle.Icon.Color = new Color32(255, 128, 255, 255);
                //boyStyle.Icon.ColorMode = ColorMode.Normal;
                //boyStyle.Icon.Icon = new IconStyle.IconLink(new Uri("http://maps.google.com/mapfiles/kml/pal3/icon21.png"));
                //boyStyle.Icon.Scale = 1.0;

                const string boyB = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=!|7fffff";
                const string girlB = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=!|ff7fff";
                const string boyD = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=X|7fffff";
                const string girlD = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=X|ff7fff";
                const string boyM = "http://chart.apis.google.com/chart?chst=d_map_pin_letter&chld=M|007f00";

                var balloon = new BalloonStyle { Text = "<![CDATA[$[description]]]>" };

                var boyStyle = new Style();
                boyStyle.Id = "boyIcon";
                boyStyle.Icon = new IconStyle();
                boyStyle.Icon.Icon = new IconStyle.IconLink(new Uri(boyB));
                boyStyle.Icon.Scale = 1.0;
                boyStyle.Balloon = new BalloonStyle { Text = "<![CDATA[$[description]]]>" };

                var girlStyle = new Style();
                girlStyle.Id = "girlIcon";
                girlStyle.Icon = new IconStyle();
                girlStyle.Icon.Icon = new IconStyle.IconLink(new Uri(girlB));
                girlStyle.Icon.Scale = 1.0;
                girlStyle.Balloon = new BalloonStyle { Text = "<![CDATA[$[description]]]>" };

                doc.AddStyle(girlStyle);
                doc.AddStyle(boyStyle);

                var boyStyleD = new Style();
                boyStyleD.Id = "boyIconD";
                boyStyleD.Icon = new IconStyle();
                boyStyleD.Icon.Icon = new IconStyle.IconLink(new Uri(boyD));
                boyStyleD.Icon.Scale = 1.0;
                boyStyleD.Balloon = balloon;

                var girlStyleD = new Style();
                girlStyleD.Id = "girlIconD";
                girlStyleD.Icon = new IconStyle();
                girlStyleD.Icon.Icon = new IconStyle.IconLink(new Uri(girlD));
                girlStyleD.Icon.Scale = 1.0;
                girlStyleD.Balloon = balloon;

                doc.AddStyle(girlStyleD);
                doc.AddStyle(boyStyleD);

                var boyStyleM = new Style();
                boyStyleM.Id = "boyIconM";
                boyStyleM.Icon = new IconStyle();
                boyStyleM.Icon.Icon = new IconStyle.IconLink(new Uri(boyM));
                boyStyleM.Icon.Scale = 1.0;
                boyStyleM.Balloon = balloon;

                doc.AddStyle(boyStyleM);

                Folder births = new Folder { Name = "Births" };
                Folder deaths = new Folder { Name = "Deaths" };
                Folder marry = new Folder { Name = "Marriages" };

                Folder timespans = new Folder { Name = "Time Spans" };
                Folder spanTo1700 = new Folder { Name = "Before 1700" };
                Folder span1700 = new Folder { Name = "1700 - 1750" };
                Folder span1750 = new Folder { Name = "1750 - 1800" };
                Folder span1800 = new Folder { Name = "1800 - 1850" };
                Folder span1850 = new Folder { Name = "1850 - 1900" };
                Folder span1900 = new Folder { Name = "1900 - 1950" };
                Folder span1950 = new Folder { Name = "1950 on" };
                List<Folder> spans = new List<Folder> { spanTo1700, span1700, span1750, span1800, span1850, span1900, span1950 };
                foreach (var folder in spans)
                {
                    timespans.AddFeature(folder);
                }

                // Add people here
                foreach (var person in people)
                {
                    if (person.BirthDate != null && !string.IsNullOrEmpty(person.BirthPlace))
                    {
                        var feat = makeBirthMark(person);
                        births.AddFeature(feat);
                        feat = makeBirthMark(person);
                        AddToSpan(spans, feat, person.BirthDate.GetValueOrDefault());
                    }

                    if (person.DeathDate != null && !string.IsNullOrEmpty(person.DeathPlace))
                    {
                        var feat = makeDeathMark(person);
                        deaths.AddFeature(feat);
                        feat = makeDeathMark(person);
                        AddToSpan(spans, feat, person.DeathDate.GetValueOrDefault());
                    }

                    if (person.Gender == Gender.Male)
                    {
                        var marrs = person.GetMarriages();
                        foreach (var relationship in marrs)
                        {
                            if (relationship.MarriageDate != null && !string.IsNullOrEmpty(relationship.MarriagePlace))
                            {
                                var feat = new Placemark();
                                //                        feat.Name = person.Name + "(B)";
                                feat.Description = new Description();
                                feat.Description.Text = string.Format("{0}\rMarried to {1}\r{2}\r{3}", person.Name,
                                                                      relationship.RelationTo.Name,
                                                                      relationship.MarriageDate.GetValueOrDefault().
                                                                          ToString("MM/dd/yyyy"),
                                                                      relationship.MarriagePlace);
                                feat.Address = relationship.MarriagePlace;

                                feat.StyleUrl = new Uri("#boyIconM", UriKind.Relative);

                                marry.AddFeature(feat);
                            }
                        }
                    }
                }

                doc.AddFeature(births);
                doc.AddFeature(deaths);
                //                doc.AddFeature(marry);
                doc.AddFeature(timespans);

                //System.Console.WriteLine("Births:" + births.Features.Count());
                //System.Console.WriteLine("Deaths:" + deaths.Features.Count());
                //foreach (var folder in spans)
                //{
                //    System.Console.WriteLine("Span:" + folder.Features.Count());
                //}

                var ser = new Serializer();
                ser.Serialize(doc);

                var filen = Path.GetFileNameWithoutExtension(gedcomFilePath);
                File.WriteAllText(filen + ".kml", ser.Xml);
            }
            catch (Exception ex)
            {
                // TODO need a better response
            }
        }

        private static Placemark makeDeathMark(Person person)
        {
            var feat = new Placemark();
            //                        feat.Name = person.Name + "(B)";
            feat.Description = new Description();
            feat.Description.Text = string.Format("{0}\r{1}\r{2}\r{3}", person.Name, "Death",
                                                  person.DeathDate.GetValueOrDefault().ToString("MM/dd/yyyy"),
                                                  person.DeathPlace);
            feat.Address = person.DeathPlace;

            feat.StyleUrl = new Uri(person.Gender == Gender.Male ? "#boyIconD" : "#girlIconD", UriKind.Relative);
            return feat;
        }

        private static Placemark makeBirthMark(Person person)
        {
            var feat = new Placemark();
            //                        feat.Name = person.Name + "(B)";
            feat.Description = new Description();
            feat.Description.Text = string.Format("{0}\r{1}\r{2}\r{3}", person.Name, "Birth",
                                                  person.BirthDate.GetValueOrDefault().ToString("MM/dd/yyyy"),
                                                  person.BirthPlace);
            feat.Address = person.BirthPlace;

            feat.StyleUrl = new Uri(person.Gender == Gender.Male ? "#boyIcon" : "#girlIcon", UriKind.Relative);
            return feat;
        }

        private static Placemark makeBirthMarkLL(Person person)
        {
            var latlong = GetCoord(person.BirthPlace);
            if (latlong == null)
                return null;

            var feat = new Placemark();
            //                        feat.Name = person.Name + "(B)";
            feat.Description = new Description();
            feat.Description.Text = string.Format("{0}\r{1}\r{2}\r{3}", person.Name, "Birth",
                                                  person.BirthDate.GetValueOrDefault().ToString("MM/dd/yyyy"),
                                                  person.BirthPlace);

            //            feat.Address = person.BirthPlace;

            var pt = new Point();
            pt.Coordinate = new Vector(latlong.Item1, latlong.Item2);
            feat.Geometry = pt;

            feat.StyleUrl = new Uri(person.Gender == Gender.Male ? "#boyIcon" : "#girlIcon", UriKind.Relative);
            return feat;
        }
    }
}
