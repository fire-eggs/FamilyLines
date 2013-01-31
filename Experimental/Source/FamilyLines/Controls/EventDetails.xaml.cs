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

using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GEDCOM.Net;
using KBS.FamilyLinesLib;

// TODO extend to provide access to Citations, media, etc?
// TODO impacted properties, e.g. birthdate -> age/agegroup/etc

namespace KBS.FamilyLines.Controls
{
    public partial class EventDetails : INotifyPropertyChanged
    {
        public EventDetails()
        {
            InitializeComponent();
            DataContext = this;
        }

        private Person _individual;
        public Person Individual
        {
            get { return _individual; }
            set 
            { 
                _individual = value;
                var events = _individual.GetEvents(EventType);
                Event = events.Count == 0 
                        ? new GedcomIndividualEvent { EventType = EventType } 
                        : events[0];

                OnPropertyChanged("IsLocked");
            }
        }

        public GedcomEvent.GedcomEventType EventType { get; set; }

        private GedcomIndividualEvent _event;

        private GedcomIndividualEvent Event
        {
            get { return _event; }
            set
            {
                _event = value;
                OnPropertyChanged("EventDateName");
                OnPropertyChanged("EventPlaceName");
                OnPropertyChanged("EventDate");
                OnPropertyChanged("EventPlace");
                OnPropertyChanged("HasEventPlace");
                OnPropertyChanged("DateDescriptor");
            }
        }

        public bool IsLocked
        {
            get { return Individual == null || Individual.IsLocked; }
        }

        public string EventDateName
        {
            get
            {
                return "Date of " + GedcomEvent.TypeToReadable(EventType);
            }
        }

        public string EventPlaceName
        {
            get
            {
                return "Place of " + GedcomEvent.TypeToReadable(EventType);
            }
        }

        public string EventPlace
        {
            get
            {
                if (Event == null || Event.Place == null)
                    return "";
                return Event.Place.Name;
            }
            set
            {
                Event.Place.Name = value;
            }
        }

        public string DateDescriptor
        {
            get { return ""; } // TBD the real descriptor AFT/BEF/ABT
            set {}
        }

        public DateTime? EventDate
        {
            get
            {
                if (Event == null || Event.Date == null)
                    return null;
                return Event.Date.DateTime1;
            }
            set
            {
                Event.Date.ParseDateString(value.ToString());
            }
        }

        public bool HasEventPlace
        {
            get
            {
                return !(string.IsNullOrEmpty(EventPlace));
            }
        }

        #region Events
        private void Label_MouseEnter(object sender, MouseEventArgs e)
        {
            Label s = (Label)sender;
            s.Foreground = Brushes.LightSteelBlue; // TODO change for theme???
        }

        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            Label s = (Label)sender;
            s.Foreground = Brushes.White; // TODO change for theme???
        }

        private void ChangeDescriptorForward(object sender, MouseButtonEventArgs e)
        {
            // TODO
        }

        private void ChangeDescriptorBackward(object sender, MouseButtonEventArgs e)
        {
            // TODO
        }

        private void ToolTip_All(object sender, ToolTipEventArgs e)
        {
            UpdateToolTip(DateEditTextBox);
            UpdateToolTip(PlaceEditTextBox);
        }

        private void SearchMapEventPlace(object sender, MouseButtonEventArgs e)
        {
            // TODO
//            SearchMap(this.family.Current.BurialPlace.ToString());
        }
        #endregion Events

        private void UpdateToolTip(TextBox box)
        {
            box.ToolTip = "TBD: filler tooltip"; // TODO real tooltip
        }

        public void Leaving()
        {
            // Make sure the data binding is updated for fields that update during LostFocus.
            if (DateEditTextBox.IsFocused)
                DateEditTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
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
