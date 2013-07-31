/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GEDCOM.Net;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for FactEditor.xaml
    /// </summary>
    public partial class FactEditor : INotifyPropertyChanged, IDataErrorInfo
    {
        public FactEditor()
        {
            InitializeComponent();
            DataContext = this; // TODO set in XAML?
        }

        private Person _target;
        public Person Target 
        { 
            get
            {
                return _target;
            }
            set
            {
                _target = value; 
                OnPropertyChanged("PName");
            }
        }

        private GEDAttribute _event;
        public GEDAttribute Event 
        { 
            get
            {
                return _event;
            }
            set
            {
                _event = value; 
                OnPropertyChanged("EventName");
                OnPropertyChanged("EventDate");
                OnPropertyChanged("EventPlace");
                OnPropertyChanged("IsLocked");
                OnPropertyChanged("Description");
                OnPropertyChanged("Address");
                OnPropertyChanged("Age");
                OnPropertyChanged("Cause");
                OnPropertyChanged("Agency");
                OnPropertyChanged("Religion");
                OnPropertyChanged("HasReligion");
                OnPropertyChanged("Certainty");
                OnPropertyChanged("Privacy");
            }
        }

        public string PName
        {
            get { return Target == null ? "" : Target.FullName; }
        }

        public string EventName
        {
            get { return Event == null ? "" : GedcomEvent.TypeToReadable(Event.Type); }
        }

        private string _localDateString;
        public string EventDate
        {
            get
            {
                if (Event == null || Event.Date == null)
                    return null;
                return Event.Date.DateString;
            }
            set
            {
                _localDateString = value;
                //                Debug.WriteLine(value);
                if (GedcomDate.TryParseDateString(value))
                {
                    Event.Date.ParseDateString(value);
                    //                    Debug.WriteLine(":" + Event.Date.DateString);
                    //                    Debug.WriteLine(":" + Event.Date.DateTime1);
                }
                OnPropertyChanged("EventDate");
                OnPropertyChanged("DateDescriptor");
            }
        }

        public bool HasEventPlace
        {
            get
            {
                return !(string.IsNullOrEmpty(EventPlace));
            }
        }

        public string EventPlace
        {
            get
            {
                if (Event == null || Event.Place == null)
                    return "";
                return Event.Place; // TODO Event.Place.Name;
            }
            set
            {
                //if (Event.Place == null)
                //    Event.Place = new GedcomPlace();
                Event.Place = value; // TODO Event.Place.Name = value;
                OnPropertyChanged("EventPlace");
                OnPropertyChanged("HasEventPlace");
            }
        }

        public bool IsLocked
        {
            get { return Target == null || Target.IsLocked; }
        }

        public string Description
        {
            get
            {
                if (Event == null || Event.Description == null)
                    return "";
                return Event.Description;
            }
            set
            {
                Event.Description = value;
                OnPropertyChanged("Description");
            }
        }

        public string Religion
        {
            get
            {
                if (Event == null || Event.ReligiousAffiliation == null)
                    return "";
                return Event.ReligiousAffiliation;
            }
            set
            {
                Event.ReligiousAffiliation = value;
                OnPropertyChanged("Religion");
            }
        }

        public string Agency
        {
            get
            {
                if (Event == null || Event.ResponsibleAgency == null)
                    return "";
                return Event.ResponsibleAgency;
            }
            set
            {
                Event.ResponsibleAgency = value;
                OnPropertyChanged("Agency");
            }
        }

        public string Address
        {
            get
            {
                if (Event == null || Event.Address == null || Event.Address.AddressLine == null)
                    return "";
                return Event.Address.AddressLine;
            }
            set
            {
                Event.Address.AddressLine = value;
                OnPropertyChanged("Address");
            }
        }

        public string Cause
        {
            get
            {
                if (Event == null || Event.Cause == null)
                    return "";
                return Event.Cause;
            }
            set
            {
                Event.Cause = value;
                OnPropertyChanged("Cause");
            }
        }

        #region routed events

        public static readonly RoutedEvent CloseButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CloseButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FactEditor));

        // Expose this event for this control's container
        public event RoutedEventHandler CloseButtonClick
        {
            add { AddHandler(CloseButtonClickEvent, value); }
            remove { RemoveHandler(CloseButtonClickEvent, value); }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseButtonClickEvent));
        }

        #endregion

        #region Events
        private void Label_MouseEnter(object sender, MouseEventArgs e)
        {
            var s = (Label)sender;
            s.Foreground = Brushes.LightSteelBlue; // TODO change for theme???
        }

        private void Label_MouseLeave(object sender, MouseEventArgs e)
        {
            var s = (Label)sender;
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
            //UpdateToolTip(DateEditTextBox);
            //UpdateToolTip(PlaceEditTextBox);
        }
        #endregion

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

        #region Implementation of IDataErrorInfo

        public string this[string columnName]
        {
            get
            {
                string result = null;

                switch (columnName)
                {
                    case "EventDate":
                        if (_localDateString != null &&
                            _localDateString.Trim().Length != 0 &&
                            !GedcomDate.TryParseDateString(_localDateString))
                            result = "invalid date";
                        break;
                    default:
                        break;
                }
                return result;
            }
        }

        public string Error
        {
            get { return null; }
        }

        #endregion

    }
}
