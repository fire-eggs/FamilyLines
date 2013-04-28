/*
 * The Model for a GEDCOM Event.
 * 
 * 1. On import from a GED file, the data from GEDCOM.Net is translated to
 *    this model.
 * 2. On load of a Family.Show .familyx file, the data from specific Person
 *    properties is translated to this model.
 * 3. Used to serialize the Family Lines .familyx file.
 * 4. The EventDetails.xaml GUI relies on this model.
 */
using System;
using System.Xml.Serialization;
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

        public GEDEvent(GedcomIndividualEvent individualEvent)
        {
            Type = individualEvent.EventType;
            Date = individualEvent.Date;
            DateDescriptor = null;
            Place = individualEvent.Place == null ? null : individualEvent.Place.Name;

            // Description is the EventName value
            Description = individualEvent.EventName;

            Age = individualEvent.Age;
            ResponsibleAgency = individualEvent.ResponsibleAgency;
            Cause = individualEvent.Cause;
            Address = individualEvent.Address;

            // TODO not sure when this applies?
            ReligiousAffiliation = individualEvent.ReligiousAffiliation;
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
        public string Description { get; set; }

        public GedcomAge Age { get; set; }
        public string ResponsibleAgency { get; set; }
        public string ReligiousAffiliation { get; set; }
        public string Cause { get; set; }
        public GedcomAddress Address { get; set; }

        // TODO additional GEDCOM event details/properties
        // Famc
        // ChangeDate
        // Certainty
        // Sources
        // Notes
        // Multimedia
    }
}
