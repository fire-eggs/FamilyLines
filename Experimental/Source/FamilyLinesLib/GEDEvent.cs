using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GEDCOM.Net;

namespace KBS.FamilyLinesLib
{
    [Serializable]
    public class GEDEvent
    {
        //private string place;
        //private DateTime? date;
        //private string dateDescriptor;
        //private string citation;
        //private string source;
        //private string link;
        //private string citationNote;
        //private string citationActualText;

        public GEDEvent() {} // TODO needed for serialization?

        public GedcomEvent.GedcomEventType Type { get; set; }
        public string Place { get; set; }
        public DateTime? Date { get; set; }
        public string DateDescriptor { get; set; }
        public string Citation { get; set; }
        public string Source { get; set; }
        public string Link { get; set; }
        public string CitationNote { get; set; }
        public string CitationActualText { get; set; }

        // Age
        // Famc
        // ChangeDate
        // Certainty
        // Cause
        // ReligiousAffiliation
        // ResponsibleAgency
        // Address
        // Sources
        // Notes
        // Multimedia




    }
}
