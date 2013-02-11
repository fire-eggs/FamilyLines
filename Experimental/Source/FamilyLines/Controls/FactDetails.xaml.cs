/*
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA
 */

using System.ComponentModel;
using System.Windows.Controls;
using GEDCOM.Net;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for FactDetails.xaml
    /// </summary>
    public partial class FactDetails : INotifyPropertyChanged
    {
        public FactDetails()
        {
            InitializeComponent();
            DataContext = this;
        }

        public GedcomEvent.GedcomEventType EventType { get; set; }

        private Person _individual;
        public Person Individual
        {
            get { return _individual; }
            set
            {
                _individual = value;
                var events = _individual.GetFacts(EventType);
                Event = events.Count == 0 ? null : events[0];

                OnPropertyChanged("IsLocked");
            }
        }

        private GEDAttribute _event;
        private GEDAttribute Event
        {
            get { return _event; }
            set
            {
                _event = value;
                OnPropertyChanged("Fact");
            }
        }

        public string FactName
        {
            get { return GedcomEvent.TypeToReadable(EventType); }
        }

        public string Fact
        {
            get
            {
                if (Event == null || string.IsNullOrEmpty(Event.Text))
                    return "";
                return Event.Text;
            }
            set 
            { 
                insureFact();
                Event.Text = value;
                OnPropertyChanged("Fact");
            }
        }

        private void ToolTip_All(object sender, ToolTipEventArgs e)
        {
            FactTextBox.ToolTip = "TBD: filler tooltip"; // TODO real tooltip
        }

        public bool IsLocked
        {
            get { return Individual == null || Individual.IsLocked; }
        }

        private void insureFact()
        {
            // The user has made a change to a property. The necessary instance
            // might not actually exist: make sure it does exist and add it to
            // the person's fact list.
            if (_event == null)
            {
                _event = new GEDAttribute();
                _event.Type = EventType;
                _individual.Facts.Add(_event);
            }
        }

        public void Leaving()
        {
            // Make sure the data binding is updated for fields that update during LostFocus.
            if (FactTextBox.IsFocused)
            {
                var res = FactTextBox.GetBindingExpression(TextBox.TextProperty);
                if (res != null)
                    res.UpdateSource();
            }
        }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// INotifyPropertyChanged requires a property called PropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fires the event for the property when it changes.
        /// </summary>
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
