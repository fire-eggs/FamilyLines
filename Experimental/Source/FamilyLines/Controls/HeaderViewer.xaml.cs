using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GEDCOM.Net;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for HeaderViewer.xaml
    /// </summary>
    public partial class HeaderViewer
    {
        private Grid owner;
        private ViewerCallback cb;
        private GedcomHeader header;

        public HeaderViewer(GedcomHeader _header)
        {
            InitializeComponent();
            DataContext = this;
            header = _header;
        }

        private string makeAddress(GedcomAddress addr)
        {
            // TODO: consider dumping blank lines
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(addr.AddressLine1);
            sb.AppendLine(addr.AddressLine2);
            sb.AppendLine(addr.AddressLine3);
            sb.Append(addr.City);
            sb.Append(", ");
            sb.Append(addr.State);
            sb.Append(" ");
            sb.AppendLine(addr.PostCode);
            sb.AppendLine(addr.Country);
            sb.AppendLine(addr.Email1);
            sb.AppendLine(addr.Email2);
            sb.AppendLine(addr.Email3);
            return sb.ToString();
        }

        public string SubmitDate
        {
            get
            {
                // TODO which date to show? source, transmission, or change?
//                return header.ChangeDate.DateString;
                return header.TransmissionDate.DateString;
            }
        }

        public string Submitter
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(header.Submitter.Name);
                sb.Append(makeAddress(header.Submitter.Address));
                return sb.ToString();
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

        public delegate void ViewerCallback();

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
            cb();
        }
    }
}
