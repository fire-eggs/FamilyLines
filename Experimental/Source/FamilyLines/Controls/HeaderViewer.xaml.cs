/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System.Text;
using System.Windows;
using System.Windows.Controls;
using GEDCOM.Net;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// A control to view (read-only) the contents of the GEDCOM header. The imported header is
    /// historic data and should not be edited. The user can enter their own data elsewhere for 
    /// when they export to GEDCOM.
    /// </summary>
    public partial class HeaderViewer
    {
        private Grid owner;
        private ViewerCallback cb;
        private readonly GedcomHeader header;

        public HeaderViewer(GedcomHeader _header)
        {
            InitializeComponent();
            DataContext = this;
            header = _header;
        }

        private string makeAddress(GedcomAddress addr)
        {
            // TODO: consider dumping blank lines?
            // TODO: collapsible portions? (e.g. address, email, phone)
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(addr.AddressLine);
            sb.AppendLine(addr.AddressLine1);
            sb.AppendLine(addr.AddressLine2);
            sb.AppendLine(addr.AddressLine3);
            sb.Append(addr.City);
            sb.Append(", ");
            sb.Append(addr.State);
            sb.Append(" ");
            sb.AppendLine(addr.PostCode);
            sb.AppendLine(addr.Country);
            sb.AppendLine(addr.Phone1);
            sb.AppendLine(addr.Email1);
            return sb.ToString();
        }

        public string SubmitDate
        {
            get
            {
                // TODO which date to show? source, transmission, or change?
                if (header.ChangeDate != null)
                    return header.ChangeDate.DateString;
                if (header.TransmissionDate != null)
                    return header.TransmissionDate.DateString;
                return "";
            }
        }

        public string Submitter
        {
            get
            {
                if (header.HasSubmitter)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(header.Submitter.Name);
                    sb.Append(makeAddress(header.Submitter.Address));
                    return sb.ToString();
                }
                return "";
            }
        }

        public string SubmitCount
        {
            get
            {
                return "" + header.Submitters.Count + " submitters";
            }
        }

        public string Copyright
        {
            get
            {
                return header.Copyright;
            }
        }

        public string HeadLanguage
        {
            get
            {
                return header.Language;
            }
        }

        public string Application
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(header.ApplicationName);
                sb.Append(" ");
                sb.Append(header.ApplicationVersion);
                return sb.ToString();
            }
        }

        public string Company
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(header.Corporation);
                if (header.CorporationAddress != null)
                    sb.Append(makeAddress(header.CorporationAddress));
                return sb.ToString();
            }
        }

        public string Source
        {
            get
            {
                return header.SourceName;
            }
        }

        public delegate void ViewerCallback(bool wasSaved);

        public static void ViewHeader(GedcomHeader header, Grid _owner, ViewerCallback _cb)
        {
            HeaderViewer hv = new HeaderViewer(header);
            hv.owner = _owner;
            hv.cb = _cb;
            hv.HorizontalAlignment = HorizontalAlignment.Center;
            hv.VerticalAlignment = VerticalAlignment.Center;
            hv.Visibility = Visibility.Visible;
            _owner.Children.Add(hv);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            owner.Children.Remove(this);

            // make the callback to the invoking code. This really, really should not
            // be null (otherwise the app misbehaves) but we'll check just in case.
            if (cb != null)
                cb(false);
        }
    }
}
