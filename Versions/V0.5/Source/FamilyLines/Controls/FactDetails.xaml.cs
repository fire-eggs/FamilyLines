/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            //FactTextBox.ToolTip = "TBD: filler tooltip"; // TODO real tooltip
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
                _event = new GEDAttribute {Type = EventType};
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
        [Localizable(false)]
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private void Click_EditAllDetails(object sender, MouseButtonEventArgs e)
        {
            insureFact();

            // fire to the main window. pass the person, which 'fact'
            var childProps = new Tuple<Person, GEDAttribute>(Individual, Event);
            var e2 = new RoutedEventArgs(Commands.EditAllFactDetails, childProps);
            RaiseEvent(e2);
        }
    }
}
