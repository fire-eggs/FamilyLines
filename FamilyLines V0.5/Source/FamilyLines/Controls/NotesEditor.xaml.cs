/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 */
using System.Windows;
using System.Windows.Controls;
using GEDCOM.Net;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for NotesEditor.xaml
    /// </summary>
    public partial class NotesEditor
    {
        public delegate void ViewerCallback(bool wasSaved);

        private Grid owner;
        private ViewerCallback cb;
        private readonly GedcomHeader head;
        private bool _wasSaved;

        public NotesEditor(GedcomHeader header)
        {
            InitializeComponent();
            head = header;
            DataContext = this;

            test.Text = head.ContentDescription == null ? "" : header.ContentDescription.Text;
            _wasSaved = false;
        }

        public static void Edit(GedcomHeader header, Grid _owner, ViewerCallback _cb)
        {
            var ctl = new NotesEditor(header);
            ctl.owner = _owner;
            ctl.cb = _cb;
            _owner.Children.Add(ctl);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            // TODO check for changes & prompt?

            owner.Children.Remove(this);

            // make the callback to the invoking code. This really, really should not
            // be null (otherwise the app misbehaves) but we'll check just in case.
            if (cb != null)
                cb(_wasSaved);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (head.ContentDescription == null)
                head.ContentDescription = new GedcomNoteRecord();
            head.ContentDescription.Text = test.Text;
            _wasSaved = true;
        }
    }
}
