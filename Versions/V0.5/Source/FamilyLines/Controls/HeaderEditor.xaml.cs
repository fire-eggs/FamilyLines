/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 */
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GEDCOM.Net;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// A window for editing GEDCOM header, submitter data for export.
    /// </summary>
    public partial class HeaderEditor
    {
        private Grid owner;
        private ViewerCallback cb;
        private readonly GedcomHeader header;

        public HeaderEditor(GedcomHeader _header)
        {
            InitializeComponent();
            DataContext = this;
            header = _header;

            // Deal with several possible null/uninitialized fields
            if (header.Submitters == null)
            {
                header.Submitters = new List<GedcomSubmitterRecord>(1);
            }

            if (header.Submitters.Count < 1)
            {
                header.Submitters.Add(new GedcomSubmitterRecord());
            }

            // Fetch current values ('close' w/o save loses changes)
            SubmitterName = header.Submitter.Name;
            Address1 = header.Submitter.Address.AddressLine ?? "";
            Address2 = header.Submitter.Address.AddressLine1 ?? "";
            Phone = header.Submitter.Address.Phone1 ?? "";
            Lang = header.Submitter.LanguagePreferences[0] ?? "";
            RFN = header.Submitter.RegisteredRFN ?? "";
            Copr = header.Copyright ?? "";
        }

        public delegate void ViewerCallback();

        public static void EditHeader(GedcomHeader header, Grid _owner, ViewerCallback _cb)
        {
            var hv = new HeaderEditor(header);
            hv.owner = _owner;
            hv.cb = _cb;
            hv.HorizontalAlignment = HorizontalAlignment.Center;
            hv.VerticalAlignment = VerticalAlignment.Center;
            hv.Visibility = Visibility.Visible;
            _owner.Children.Add(hv);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // TODO check for changes & prompt?

            owner.Children.Remove(this);

            // make the callback to the invoking code. This really, really should not
            // be null (otherwise the app misbehaves) but we'll check just in case.
            if (cb != null)
                cb();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // save current values
            header.Submitter.Name = SubmitterName.Trim();
            header.Submitter.Address.AddressLine = Address1.Trim();
            header.Submitter.Address.AddressLine1 = Address2.Trim();
            header.Submitter.Address.Phone1 = Phone.Trim();
            header.Submitter.LanguagePreferences[0] = Lang.Trim();
            header.Submitter.RegisteredRFN = RFN.Trim();
            header.Copyright = Copr.Trim();
        }

        public string SubmitterName { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Phone { get; set; }
        public string Lang { get; set; }
        public string RFN { get; set; }
        public string Copr { get; set; }

    }

}
