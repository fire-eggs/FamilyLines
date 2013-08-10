/*
 * Family.Show derived code provided under MS-PL license.
 * 
 * Imports data from a GEDCOM file to the People collection. GEDCOM Ids are converted to GUIDs.
 *
 * More information on the GEDCOM format is at http://en.wikipedia.org/wiki/Gedcom
 * and http://homepages.rootsweb.ancestry.com/~pmcbride/gedcom/55gctoc.htm
 * 
 * This class has a few modifications to use _frel and _mrel which are common proprietary tags
 * used by other programs for adoption
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GEDCOM.Net;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Import data from a GEDCOM file to the Person collection.
    /// </summary>
    public class GedcomImport
    {
        class Finder
        {
            private Hashtable _dex = new Hashtable();
            public void Add(Person p)
            {
                if (p.Individual == null)
                    return;
                _dex.Add(p.Individual.XRefID, p);
            }

            public Person Find(string id)
            {
                return (Person) _dex[id];
            }
        }

        private Finder _finder;

        #region fields

        // The collection to add entries.
        private PeopleCollection people;
        private SourceCollection sources;
        private RepositoryCollection repositories;
        private GedcomHeader header;

        // GEDCOM.Net state
        private GedcomRecordReader _reader;
        private GedcomDatabase _database;

        #endregion

        /// <summary>
        /// Populate the people collection with information from the GEDCOM file.
        /// </summary>
        public bool Import(out GedcomHeader _header, PeopleCollection peopleCollection, SourceCollection sourceCollection, 
                           RepositoryCollection repositoryCollection, string gedcomFilePath, bool disableCharacterCheck)
        {
            _header = null;
            _finder = new Finder();

            // Clear current content.
            peopleCollection.Clear();
            sourceCollection.Clear();
            repositoryCollection.Clear();

            people = peopleCollection;
            sources = sourceCollection;
            repositories = repositoryCollection;

            // TODO switch to an asynchronous process - need a "wait" mechanism
            //_reader = new BackgroundGedcomRecordReader();
            //_reader.Completed += reader_Completed;
            //_reader.ProgressChanged += reader_ProgressChanged;

            _reader = new GedcomRecordReader();
            if (!_reader.ReadGedcom(gedcomFilePath))
                return false;
            reader_Completed(null, null);

            _header = header;
            return true;
        }

        //void reader_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        //{
        //    // TODO
        //}

// ReSharper disable UnusedParameter.Local - pending asynchronous process
        void reader_Completed(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            _database = _reader.Database;

            _finder = new Finder();
            header = ImportHeader();

            foreach (GedcomIndividualRecord t in _database.Individuals)
            {
                Person aPerson = new Person(t);
                people.Add(aPerson);
                _finder.Add(aPerson);
            }

//#if DEBUG
//            startLog(@"c:\temp\kbrlog_gn.txt");
//#endif
            ImportFamiliesGN();
            ImportSourcesGN();
            ImportRepositoriesGN();

            foreach (Person p in people)
            {
                p.Validate();
            }
            people.RebuildTrees(); // set up the initial state of trees

            _reader.importLog("complete import","");
        }
// ReSharper restore UnusedParameter.Local

        /// <summary>
        /// Translate GEDCOM.net sources into Family.Show data
        /// </summary>
        private void ImportSourcesGN()
        {
            foreach (var sauce in _database.Sources)
            {
                Source source = new Source();

                source.Id = sauce.XRefID;
                source.SourceName = sauce.Title;

                source.SourceAuthor = sauce.Originator;
                source.SourcePublisher = sauce.PublicationFacts;

                // TODO a brute-force smush of multiple notes into one string: revisit for improvement/completeness
                source.SourceNote = sauce.Notes.Aggregate("", (current, note) => current + (note + " "));

                // TODO a brute-force smush of multiple repositories into one string: revisit for improvement/completeness
                source.SourceRepository = "";
                foreach (var reposCite in sauce.RepositoryCitations)
                {
                    source.SourceRepository += reposCite.Repository + " ";
                }

                // TODO add storage for additional GEDCOM source data: Text, Agency, etc.

                sources.Add(source);
            }
        }

        /// <summary>
        /// Translate GEDCOM.net repositories into Family.Show data
        /// </summary>
        private void ImportRepositoriesGN()
        {
            foreach (var gnrepos in _database.Repositories)
            {
                Repository repository = new Repository();

                repository.Id = gnrepos.XRefID;
                repository.RepositoryName = gnrepos.Name;
                repository.RepositoryAddress = gnrepos.Address == null ? "" : gnrepos.Address.ToString(); // TODO gedcomaddress is an object not a string
                repositories.Add(repository);
            }            
        }

#if DEBUG
        //private string logName;
        //private void startLog(string logname)
        //{
        //    try
        //    {
        //        logName = logname;
        //        StreamWriter log = new StreamWriter(logName, false);
        //        log.WriteLine(DateTime.Now);
        //        log.WriteLine("=========================");
        //        log.Close();
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        private void kbrLog(string text, Person husbandPerson, Person childPerson, object husbandModifier)
        {
        //    try
        //    {
        //        StreamWriter log = new StreamWriter(logName, true);
        //        log.WriteLine("{0} : '{2}' to '{1}' as {3}", text, husbandPerson.Name, childPerson.Name, husbandModifier);
        //        log.Close();
        //    }
        //    catch (Exception)
        //    {
        //    }
        }
#else
        private void kbrLog(string text, Person husbandPerson, Person childPerson, object husbandModifier)
        {}
#endif

        private void ImportChildGN(GedcomIndividualRecord child, GedcomIndividualRecord daddy, GedcomIndividualRecord mommy)
        {
            var childP = _finder.Find(child.XRefID);

            ParentChildModifier dadMod = ParentChildModifier.Natural;
            ParentChildModifier momMod = ParentChildModifier.Natural;

            // TODO FOST event?

            foreach (GedcomIndividualEvent indiEv in child.Events)
            {
                // if (indiEv.Famc == fambly.XRefID) TODO this is not working with PAF import, why???
                {
                    switch (indiEv.EventType)
                    {
                        case GedcomEvent.GedcomEventType.ADOP:
                            switch (indiEv.AdoptedBy)
                            {
                                case GedcomAdoptionType.Husband:
                                    dadMod = ParentChildModifier.Adopted;
                                    break;
                                case GedcomAdoptionType.Wife:
                                    momMod = ParentChildModifier.Adopted;
                                    break;
                                default:
                                    // default is both as well, has to be adopted by someone if
                                    // there is an event on the family.
                                    dadMod = momMod = ParentChildModifier.Adopted;
                                    break;
                            }
                            break;
                    }
                }
            }

            List<Person> firstParentChildren = new List<Person>();
            if (daddy != null)
            {
                var daddyP = _finder.Find(daddy.XRefID);
                kbrLog("AddChild", daddyP, childP, dadMod);
                people.AddChild(daddyP, childP, dadMod);
                firstParentChildren = new List<Person>(daddyP.NaturalChildren);
            }

            List<Person> secondParentChildren = new List<Person>();
            if (mommy != null)
            {
                var mommyP = _finder.Find(mommy.XRefID);
                kbrLog("AddChild", mommyP, childP, momMod);
                people.AddChild(mommyP, childP, momMod);
                secondParentChildren = new List<Person>(mommyP.NaturalChildren);
            }

            // Determine natural siblings

            // Combined children list that is returned.
            List<Person> naturalChildren = new List<Person>();

            // Go through and add the children that have both parents.            
            foreach (Person child1 in firstParentChildren)
            {
                if (secondParentChildren.Contains(child1))
                    naturalChildren.Add(child1);
            }

            // Go through and add natural siblings
            foreach (Person s in naturalChildren)
            {
                if (s != childP && momMod == ParentChildModifier.Natural && dadMod == ParentChildModifier.Natural)
                    people.AddSibling(childP, s);
            }

        }

        // families from Gedcom.NET
        private void ImportFamiliesGN()
        {
            foreach (var fambly in _database.Families)
            {
                GedcomIndividualRecord hubby = fambly.Husband == null ? null : _database[fambly.Husband] as GedcomIndividualRecord;
                GedcomIndividualRecord wifey = fambly.Wife == null ? null : _database[fambly.Wife] as GedcomIndividualRecord;

                ImportMarriageGN(fambly, hubby, wifey);

                foreach (var childId in fambly.Children)
                {
                    var child = _database[childId] as GedcomIndividualRecord;
                    ImportChildGN(child, hubby, wifey);
                }
            }

            // TODO diagram doesn't show child as adopted initially but changes on refresh?
        }

        //// TODO slow with large family: turn into a HashMap?
        //private Person HackFind(string XRefID)
        //{
        //    foreach (var aperson in people)
        //    {
        //        if (aperson.Individual != null && aperson.Individual.XRefID == XRefID)
        //            return aperson;
        //    }
        //    return null;
        //}

        private void ImportMarriageGN(GedcomFamilyRecord fambly, GedcomIndividualRecord hubby, GedcomIndividualRecord wifey)
        {
            if (hubby == null || wifey == null)
                return;

            // TODO hubby or wifey null

            var hubbyP = _finder.Find(hubby.XRefID);
            var wifeyP = _finder.Find(wifey.XRefID);

            // TODO: assuming only one marriage/divorce for a couple, i.e. MARR+DIV+MARR would be possible? How does this appear in GED?

            if (fambly.Marriage == null && fambly.Divorce == null)
            {
                // A FAM record has been specified, but no marriage/divorce data.
                var wifeMarriage = new SpouseRelationship(hubbyP, SpouseModifier.Current);
                var husbandMarriage = new SpouseRelationship(wifeyP, SpouseModifier.Current);

                wifeyP.Relationships.Add(wifeMarriage);
                hubbyP.Relationships.Add(husbandMarriage);

                kbrLog("Simple marriage", hubbyP, wifeyP, "");
                return;
            }

            string marrPlace = null;
            string marrSrc = null;
            DateTime? marrDate = null;
            string divPlace = null;
            string divSrc = null;
            DateTime? divDate = null;
            string marrCitationActualText = null;
            string marrCitationNote = null;

            SpouseModifier status = SpouseModifier.Current;
            if (fambly.Marriage != null)
            {
                marrPlace = fambly.Marriage.Place == null ? null : fambly.Marriage.Place.Name; // TODO smart property
                marrDate = fambly.Marriage.Date == null ? null : fambly.Marriage.Date.DateTime1; // TODO smart property

                // TODO duplicated in Person
                if (fambly.Marriage.Sources != null && fambly.Marriage.Sources.Count > 0)
                {
                    var src = fambly.Marriage.Sources[0];
                    marrCitationActualText = src.Text;
                    var src2 = src.Database[src.Source] as GedcomSourceRecord;
                    if (src2 != null)
                    {
                        marrSrc = src2.Title;
                    }
                    if (src.Notes != null && src.Notes.Count > 0)
                    {
                        marrCitationNote = src.Notes[0];
                    }
                }
            }
            if (fambly.Divorce != null)
            {
                divPlace = fambly.Divorce.Place == null ? null : fambly.Divorce.Place.Name; // TODO smart property
                divSrc = (fambly.Divorce.Sources.Count > 1) ? fambly.Divorce.Sources[0].Text : ""; // TODO smart property
                divDate = fambly.Divorce.Date == null ? null : fambly.Divorce.Date.DateTime1; // TODO smart property
                status = SpouseModifier.Former;
            }

            // TODO Citation : MARR/SOURCE/PAGE
            // TODO Citationtext : MARR/SOURCE/DATA/TEXT
            // TODO Link/media 
            // TODO Note : MARR/SOURCE/NOTE

            // TODO incorporate the event into the SpouseRelationship

            var existH = hubbyP.GetSpouseRelationship(wifeyP);
            if (existH != null)
            {
                kbrLog("relationship exists", hubbyP, wifeyP, "");
                hubbyP.Relationships.Remove(existH);
            }

            {
                var marriage = new SpouseRelationship(wifeyP, status);
                marriage.MarriageDate = marrDate;
                marriage.MarriagePlace = marrPlace;
                marriage.MarriageSource = marrSrc;
                marriage.MarriageCitationActualText = marrCitationActualText;
                marriage.MarriageCitationNote = marrCitationNote;

                marriage.DivorceDate = divDate;
                marriage.DivorceSource = divSrc;
                marriage.DivorcePlace = divPlace;
                hubbyP.Relationships.Add(marriage);
                kbrLog("AddSpouse", hubbyP, wifeyP, status);
            }
            var existW = wifeyP.GetSpouseRelationship(hubbyP);
            if (existW != null)
            {
                kbrLog("relationship exists", wifeyP, hubbyP, "");
                wifeyP.Relationships.Remove(existW);
            }
            {
                var marriage = new SpouseRelationship(hubbyP, status);
                marriage.MarriageDate = marrDate;
                marriage.MarriagePlace = marrPlace;
                marriage.MarriageSource = marrSrc;
                marriage.MarriageCitationActualText = marrCitationActualText;
                marriage.MarriageCitationNote = marrCitationNote;

                marriage.DivorceDate = divDate;
                marriage.DivorceSource = divSrc;
                marriage.DivorcePlace = divPlace;
                wifeyP.Relationships.Add(marriage);
                kbrLog("AddSpouse", wifeyP, hubbyP, status);
            }
        }
		
        ///// <summary>
        ///// Often programs do not store links correctly.
        ///// Method to extract the first url link out of citation note.
        ///// </summary>
        //private static string GetLink(string Note)
        //{

        //    if (!string.IsNullOrEmpty(Note))
        //    {
        //        Array Link = Note.Split();

        //        foreach (string s in Link)
        //        {
        //            if ((s.StartsWith("http://") || s.StartsWith("www.")))
        //                return s;  //only extract one link
        //        }
              
        //        return string.Empty;
        //    }
        //    else
        //        return string.Empty;
        //}

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        //private static string GetValue(XmlNode node, string xpath)
        //{
        //    try 
        //    { 
        //        XmlNode valueNode = node.SelectSingleNode(xpath);

        //        if (valueNode != null)
        //                return valueNode.Attributes["Value"].Value.Trim();
        //    }
        //    catch 
        //    {
        //         //Invalid line, keep processing the file.
        //    }
        //    return string.Empty;
        //}

        /// <summary>
        /// Bring the GEDCOM header information in
        /// </summary>
        private GedcomHeader ImportHeader()
        {
            if (_database.Header == null)
                return null;
            return _database.Header.Copy();
        }
    }

}

// ReSharper restore SpecifyACultureInStringConversionExplicitly
