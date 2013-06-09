/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GEDCOM.Net;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// View *all* the facts / events for the given person
    /// </summary>
    public partial class ViewAllFacts : INotifyPropertyChanged
    {
        #region Event and Fact display for adding a new event/fact
        private readonly List<string> EventList = new List<string>();
        private readonly List<string> FactList = new List<string>();

        public GedcomEvent.GedcomEventType[] FactsOnly = new[]
                                                             {
                                                            GedcomEvent.GedcomEventType.GenericFact,
                                                            GedcomEvent.GedcomEventType.CASTFact,
                                                            GedcomEvent.GedcomEventType.DSCRFact,
                                                            GedcomEvent.GedcomEventType.EDUCFact,
                                                            GedcomEvent.GedcomEventType.IDNOFact,
                                                            GedcomEvent.GedcomEventType.NATIFact,
                                                            GedcomEvent.GedcomEventType.NCHIFact,
                                                            GedcomEvent.GedcomEventType.NMRFact,
                                                            GedcomEvent.GedcomEventType.OCCUFact,
                                                            GedcomEvent.GedcomEventType.PROPFact,
                                                            GedcomEvent.GedcomEventType.RELIFact,
                                                            GedcomEvent.GedcomEventType.RESIFact,
                                                            GedcomEvent.GedcomEventType.SSNFact,
                                                            GedcomEvent.GedcomEventType.TITLFact,
                                                            GedcomEvent.GedcomEventType.Custom
                                                             };

        public GedcomEvent.GedcomEventType[] IndivEventsOnly = new[]
                                                                   {
                                                                    GedcomEvent.GedcomEventType.BIRT,
                                                                    GedcomEvent.GedcomEventType.DEAT,
                                                                    GedcomEvent.GedcomEventType.BURI,
                                                                    GedcomEvent.GedcomEventType.CHR,
                                                                    GedcomEvent.GedcomEventType.CREM,
                                                                    GedcomEvent.GedcomEventType.ADOP,
                                                                    GedcomEvent.GedcomEventType.BAPM,
                                                                    GedcomEvent.GedcomEventType.BARM,
                                                                    GedcomEvent.GedcomEventType.BASM,
                                                                    GedcomEvent.GedcomEventType.BLES,
                                                                    GedcomEvent.GedcomEventType.CHRA,
                                                                    GedcomEvent.GedcomEventType.CONF,
                                                                    GedcomEvent.GedcomEventType.FCOM,
                                                                    GedcomEvent.GedcomEventType.ORDN,
                                                                    GedcomEvent.GedcomEventType.NATU,
                                                                    GedcomEvent.GedcomEventType.EMIG,
                                                                    GedcomEvent.GedcomEventType.IMMI,
                                                                    GedcomEvent.GedcomEventType.CENS,
                                                                    GedcomEvent.GedcomEventType.PROB,
                                                                    GedcomEvent.GedcomEventType.WILL,
                                                                    GedcomEvent.GedcomEventType.GRAD,
                                                                    GedcomEvent.GedcomEventType.RETI,
                                                                    GedcomEvent.GedcomEventType.Custom
                                                                   };

        private void initLists()
        {
            foreach (GedcomEvent.GedcomEventType enumVal in IndivEventsOnly)
            {
                string val = GedcomEvent.TypeToReadable(enumVal);
                if (!string.IsNullOrEmpty(val))
                    EventList.Add(val);
            }
            foreach (GedcomEvent.GedcomEventType enumVal in FactsOnly)
            {
                string val = GedcomEvent.TypeToReadable(enumVal);
                if (!string.IsNullOrEmpty(val))
                    FactList.Add(val);
            }
        }

        #endregion

        private Person _target;

        public ViewAllFacts()
        {
            InitializeComponent();
            DataContext = this; // TODO set in XAML?
            initLists();
        }

        /// <summary>
        /// The person whose facts/events we're to view. Changing the
        /// person needs to force the title bar, grid, etc to update.
        /// </summary>
        public Person Target
        {
            get
            {
                return _target;
            }
            set
            {
                _target = value;
                ClearInputs();
                OnPropertyChanged("PName");
                OnPropertyChanged("Facts");
                OnPropertyChanged("Events");

                setButtonState(OpState.NoSel);
            }
        }

        /// <summary>
        /// This toggles between Event mode and Fact mode
        /// </summary>
        public bool ShowFacts
        {
            set
            {
                if (value)
                {
                    var b = new Binding("Facts");
                    DisplayGrid.SetBinding(ItemsControl.ItemsSourceProperty, b);
                    eventPick.ItemsSource = FactList;
                }
                else
                {
                    var b = new Binding("Events");
                    DisplayGrid.SetBinding(ItemsControl.ItemsSourceProperty, b);
                    eventPick.ItemsSource = EventList;
                }
            }
        }

        public string PName
        {
            get
            {
                return Target == null ? "" : Target.FullName;
            }
        }

        public ObservableCollection<GEDEvent> Events
        {
            get
            {
                return Target == null ? null : Target.Events;
            }
        }

        public ObservableCollection<GEDAttribute> Facts
        {
            get
            {
                return Target == null ? null : Target.Facts;
            }
        }

        #region routed events

        public static readonly RoutedEvent CloseButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CloseButtonClick", RoutingStrategy.Bubble, typeof (RoutedEventHandler), typeof (ViewAllFacts));

        private GEDEvent _activeEvent;

        // Expose this event for this control's container
        public event RoutedEventHandler CloseButtonClick
        {
            add
            {
                AddHandler(CloseButtonClickEvent, value);
            }
            remove
            {
                RemoveHandler(CloseButtonClickEvent, value);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseButtonClickEvent));
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
        public virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        // User has selected an entry in the grid. Allow delete, edit
        private void DisplayGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _activeEvent = DisplayGrid.SelectedItem as GEDEvent;

            // hide the event picker combobox
            eventName.Visibility = Visibility.Visible;
            eventPick.Visibility = Visibility.Collapsed;

            if (_activeEvent == null) // true when the blank line in the grid is selected
            {
                ClearInputs();
                setButtonState(OpState.NoSel);
                return;
            }

            setButtonState(OpState.ValidSel);
            resetData();
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            DisplayGrid.SelectedItem = null;
            _activeEvent = null;
            resetData();
            setButtonState(OpState.AddClick);
        }

        private void delBtn_Click(object sender, RoutedEventArgs e)
        {
            // delete the current event/fact from the person (with confirmation)
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            // when adding, clear all edits. when editing, reset all edits to original values
            resetData();
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            // save all edits
        }

        private void resetData()
        {
            if (_activeEvent == null) // true if adding [careful with invalid selection case!]
            {
                ClearInputs();
                txtDate.Text = "new date";
                txtPlace.Text = "new place";
                txtDesc.Text = "new description";
                txtAddress.Text = "new address";

                eventName.Visibility = Visibility.Collapsed;
                eventPick.Visibility = Visibility.Visible;
                // TODO should event picker be reset to unselected?
                return;
            }

            eventName.Content = _activeEvent.EventName;
            txtDate.Text = _activeEvent.Date == null ? "" : _activeEvent.Date.DateString;
            txtPlace.Text = _activeEvent.Place;
            txtDesc.Text = _activeEvent.Description;
            txtAddress.Text = _activeEvent.Address == null ? "" : _activeEvent.Address.AddressLine;
            txtAge.Text = _activeEvent.Age == null ? "" : _activeEvent.Age.Years.ToString();
            txtAgency.Text = _activeEvent.ResponsibleAgency;
            txtCause.Text = _activeEvent.Cause;
            txtCertainty.Text = ""; // TODO don't have certainty data
        }

        private void ClearInputs()
        {
            eventName.Content = "";
            eventName.Visibility = Visibility.Visible;
            eventPick.Visibility = Visibility.Collapsed;

            txtDate.Text = "";
            txtPlace.Text = "";
            txtDesc.Text = "";
            txtAddress.Text = "";
            txtAge.Text = "";
            txtAgency.Text = "";
            txtCause.Text = "";
            txtCertainty.Text = "";
        }

        private void setButtonState(OpState state)
        {
            switch (state)
            {
                case OpState.NoSel:
                    addBtn.Visibility = Visibility.Visible;
                    delBtn.Visibility = Visibility.Hidden;
                    resetBtn.Visibility = Visibility.Hidden;
                    saveBtn.Visibility = Visibility.Hidden;
                    break;
                case OpState.AddClick:
                    addBtn.Visibility = Visibility.Hidden;
                    delBtn.Visibility = Visibility.Hidden;
                    resetBtn.Visibility = Visibility.Visible;
                    saveBtn.Visibility = Visibility.Visible;
                    break;
                case OpState.ValidSel:
                    addBtn.Visibility = Visibility.Visible;
                    delBtn.Visibility = Visibility.Visible;
                    resetBtn.Visibility = Visibility.Visible;
                    saveBtn.Visibility = Visibility.Visible;
                    break;
            }
        }

        #region Nested type: OpState

        private enum OpState
        {
            NoSel,
            AddClick,
            ValidSel
        };

        #endregion

        //private void ResetColumns(object sender, RoutedEventArgs routedEventArgs)
        //{
        //    foreach (var column in DisplayGrid.Columns)
        //    {
        //        column.MinWidth = column.ActualWidth;
        //        column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        //    }
        //}
    }
}