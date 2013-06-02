/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.Collections.Generic;
using KBS.FamilyLinesLib.Properties;

namespace KBS.FamilyLinesLib
{
    public class PeopleReport : HTMLReport
    {
        public PeopleReport(string outPath, IEnumerable<Person> people)
            : base(outPath, people)
        {
            
        }

        public PeopleReport(string fileName, IEnumerable<Person> family, IEnumerable<Source> sources, IEnumerable<Repository> repositories) 
            : base(fileName, family)
        {
        }

        public void generateReport(bool showHide=false)
        {
            outputHeader("People Report"); // TODO surname/header data?
            showHideScript();
            outputCSS();

            outputBody(showHide);

            subHeader();

            startTable();
            peopleRows();
            finishTable();

            outputFooter();

        }

        // report specific (# columns, etc)
        private void peopleRows()
        {
            foreach (var person in people)
            {
                // TODO handle privacy
                // For each person: 
                // 1. Output a <tr> with id

                var part1 = string.Format(Resources.PeopleReportTRFormat, person.Id);

                // 1a. Output a <td> with name & info
                var part2 = string.Format(person.IsLiving ? Resources.PeopleReportPersonAliveTD :
                                                            Resources.PeopleReportPersonTD,
                                          person.LastName, person.FirstName,
                                          doDate(person.BirthDateDescriptor, person.BirthDate),
                                          doDate(person.DeathDateDescriptor, person.DeathDate), person.Age);

                // 1b. output event/fact/note links as necessary
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

                // 2. Events
                doEvents(person);

                // 3. Facts
                doFacts(person);

                // 4. Note

            }

        }

        private void doEvents(Person person)
        {
            if (!person.HasEvents)
                return;
            tw.WriteLine(string.Format("<tr id=\"event_id_{0}\" class=\"noteshow\">", person.Id));
            tw.WriteLine("<td colspan=\"9\">");
            tw.WriteLine("<table class=\"event\">");
            tw.WriteLine("<thead><th width=\"15%\">Event</th><th width=\"20%\">Date</th><th>Place</th><th width=\"10%\">Age</th></thead>");

            foreach (var gedEvent in person.Events)
            {
                tw.WriteLine(string.Format("<tr><td>{0}</td><td class=\"age\">{1}</td><td>{2}</td><td class=\"age\">{3}</td></tr>",
                    gedEvent.EventName, gedEvent.Date.DateString,
                    gedEvent.Place, gedEvent.Age));
            }
            tw.WriteLine("</table></td></tr>");
        }

        private void doFacts(Person person)
        {
            if (!person.HasFacts)
                return;
            tw.WriteLine(string.Format("<tr id=\"event_id_{0}\" class=\"noteshow\">", person.Id));
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
