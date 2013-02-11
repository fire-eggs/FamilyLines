/*
 * The Model for a GEDCOM Attribute.
 * 
 * 1. On import from a GED file, the data from GEDCOM.Net is translated to
 *    this model (GEDCOM.Net calls these 'facts').
 * 2. On load of a Family.Show .familyx file, the data from specific Person
 *    properties is translated to this model.
 * 3. Used to serialize the Family Lines .familyx file.
 * 4. The AttributeDetails.xaml GUI relies on this model.
 */
using System;
using GEDCOM.Net;

namespace KBS.FamilyLinesLib
{
    [Serializable]
    public class GEDAttribute : GEDEvent
    {
        public GEDAttribute()
        {
        }

        public GEDAttribute(GedcomIndividualEvent individualEvent) : base(individualEvent)
        {
            Text = individualEvent.EventName;
        }

        public string Text { get; set; }

        // TODO: custom support: name of attribute, custom GED tag

    }
}
