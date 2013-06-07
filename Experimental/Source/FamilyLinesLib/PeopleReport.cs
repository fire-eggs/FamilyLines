/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 * This is a variant on the "Family Show V4" HTML report. This variant has been changed to contain sub-grids
 * for event and fact data. 
 */
using System;
using System.Collections.Generic;
using KBS.FamilyLinesLib.Properties;

namespace KBS.FamilyLinesLib
{
    public class PeopleReport : HTMLReport
    {
        private IEnumerable<Source> _sources;
        private IEnumerable<Repository> _repositories;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outPath">destination filename for the report</param>
        /// <param name="people">the list of people to include in the report</param>
        /// <param name="sources"></param>
        /// <param name="repositories"></param>
        public PeopleReport(string outPath, IEnumerable<Person> people, IEnumerable<Source> sources, IEnumerable<Repository> repositories) 
            : base(outPath, people)
        {
            _sources = sources;
            _repositories = repositories;
        }

        /// <summary>
        /// Generate the report. Assumes that openReport() has been invoked.
        /// TODO could this become even more generic, with title and peoplerows as "callbacks"?
        /// </summary>
        /// <param name="showHide">flag for generating Show/Hide javascript</param>
        public void generateReport(bool showHide)
        {
            outputHeader("People Report"); // TODO surname/header data?
            showHideScript();
            outputCSS();

            outputBody("", showHide, false);

            subHeader();

            startTable();
            peopleRows();
            finishTable();

            outputFooter();

            tw.Close(); // TODO make the consumer call this? closeReport?
        }

        // report specific (# columns, etc)
        private void peopleRows()
        {
            foreach (var person in people)
            {
                // For each person: 
                // 1. Output a <tr> with id
                var part1 = string.Format(Resources.PeopleReportTRFormat, person.Id);

                // Privacy tests
                if (person.IsLiving && Privacy && person.Restriction != Restriction.Private) //quick privacy option
                {
                    tw.WriteLine(part1);
                    tw.WriteLine("<td class=\"people\" colspan=\"7\">" + Resources.Living + " " + person.LastName + "</td><td /></tr>");
                    continue;
                }

                if (person.Restriction == Restriction.Private) //a private record should not be exported
                {
                    tw.WriteLine(part1);
                    tw.WriteLine("<td class=\"people\" colspan=\"7\">" + Resources.PrivateRecord + "</td><td /></tr>");
                    continue;
                }

                // 1a. Output a <td> with name & info
                var part2 = string.Format(person.IsLiving ? Resources.PeopleReportPersonAliveTD :
                                                            Resources.PeopleReportPersonTD,
                                          person.LastName, person.FirstName,
                                          doDate(person.BirthDateDescriptor, person.BirthDate),
                                          doDate(person.DeathDateDescriptor, person.DeathDate), person.Age);

                // 1b. output events/facts/notes links as necessary
                var part3a = person.HasEvents ? string.Format(Resources.PeopleReportEventLink, person.Id) : "";
                var part3b = person.HasFacts ? string.Format(Resources.PeopleReportFactLink, person.Id) : "";
                var part3c = person.HasNote ? string.Format(Resources.PeopleReportNoteLink, person.Id) : "";

                tw.WriteLine(part1);
                tw.WriteLine(part2);
                tw.Write("<td>");
                tw.Write(part3a);
                tw.Write(part3b);
                tw.Write(part3c);
                tw.WriteLine("</td>");
                tw.WriteLine("</tr>");

                doEvents(person);
                doFacts(person);
                doNotes(person);
            }
        }

        private void doEvents(Person person)
        {
            if (!person.HasEvents)
                return;
            tw.WriteLine(string.Format("<tr id=\"event_id_{0}\" class=\"eventshow\">", person.Id));
            tw.WriteLine("<td colspan=\"9\">");
            tw.WriteLine("<table class=\"event\">");
            tw.WriteLine("<thead><th width=\"15%\">Event</th><th width=\"20%\">Date</th><th>Place</th><th width=\"10%\">Age</th></thead>");

            int i = 0;
            foreach (var gedEvent in person.Events)
            {
                tw.Write("<tr" + ((i % 2 == 1) ? " class=\"odd\">" : ">"));
                tw.WriteLine(string.Format("<td>{0}</td><td class=\"age\">{1}</td><td>{2}</td><td class=\"age\">{3}</td></tr>",
                    gedEvent.EventName, gedEvent.Date == null ? "?" : gedEvent.Date.DateString,
                    gedEvent.Place, gedEvent.Age));
                i++;
            }
            tw.WriteLine("</table></td></tr>");
        }

        private void doFacts(Person person)
        {
            if (!person.HasFacts)
                return;
            tw.WriteLine(string.Format("<tr id=\"fact_id_{0}\" class=\"factshow\">", person.Id));
            tw.WriteLine("<td colspan=\"9\">");
            tw.WriteLine("<table class=\"event\" rules=\"all\" frame=\"box\">");
            tw.WriteLine("<thead><th width=\"15%\">Fact</th><th width=\"20%\">Date</th><th>Details</th><th>Place</th><th width=\"10%\">Age</th></thead>");

            foreach (var gedEvent in person.Facts)
            {
                tw.WriteLine(string.Format("<tr><td>{0}</td><td class=\"age\">{1}</td><td>{2}</td><td>{3}</td><td class=\"age\">{4}</td></tr>",
                    gedEvent.EventName, gedEvent.Date.DateString,
                    gedEvent.Description,
                    gedEvent.Place, gedEvent.Age));
            }
            tw.WriteLine("</table></td></tr>");
        }

        // TODO currently only handles a single note, should? become multiple
        private void doNotes(Person person)
        {
            if (!person.HasNote)
                return;
            tw.WriteLine(string.Format(Resources.PeopleReportNoteTR, person.Id));
            tw.WriteLine(string.Format(Resources.PeopleReportNoteTD, person.Note)); // TODO: note may need to be massaged for proper HTML display
        }

        private string doDate(string dateDescriptor, DateTime? adate)
        {
            var dates = adate == null ? "" : adate.Value.ToString("dd-MMM-yyyy");
            var dates2 = string.Format(Resources.GEDCOMDate, dateDescriptor, 
                string.IsNullOrEmpty(dateDescriptor) ? "" : " ", dates).Trim();
            return (string.IsNullOrEmpty(dates2)) ? "?" : dates2;
        }

        // report specific (# columns etc)
        private void startTable()
        {
            tw.WriteLine("<table id=\"peopletable\" class=\"people\">");
        }

        private void subHeader()
        {
            tw.WriteLine("<h2>Family Lines</h2>");
        }

    }
}
