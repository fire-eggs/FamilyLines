using System;
using GEDCOM.Net;

namespace KBS.FamilyLinesLib
{
    [Serializable]
    public class GEDEvent
    {
        public GEDEvent()
        {
            Date = new GedcomDate();
        }

        public GedcomEvent.GedcomEventType Type { get; set; }
        public string Place { get; set; }
        public GedcomDate Date { get; set; }
        public string DateDescriptor { get; set; }
        public string Citation { get; set; }
        public string Source { get; set; }
        public string Link { get; set; }
        public string CitationNote { get; set; }
        public string CitationActualText { get; set; }

        // TODO additional GEDCOM event details/properties
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
