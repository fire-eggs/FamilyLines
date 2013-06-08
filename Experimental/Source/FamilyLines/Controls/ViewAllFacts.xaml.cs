using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// View *all* the facts / events for the given person
    /// </summary>
    public partial class ViewAllFacts : INotifyPropertyChanged
    {
        public ViewAllFacts()
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
                ClearInputs();
                OnPropertyChanged("PName");
                OnPropertyChanged("Facts");
                OnPropertyChanged("Events");

                setButtonState(OpState.NoSel);
            }
        }

        public bool ShowFacts
        {
            set
            {
                if (value)
                {
                    Binding b = new Binding("Facts");
                    DisplayGrid.SetBinding(ItemsControl.ItemsSourceProperty, b);
                }
                else
                {
                    Binding b = new Binding("Events");
                    DisplayGrid.SetBinding(ItemsControl.ItemsSourceProperty, b);
                }
            }
        }

        public string PName
        {
            get { return Target == null ? "" : Target.FullName; }
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
            "CloseButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ViewAllFacts));

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
            var sel = DisplayGrid.SelectedItem as GEDEvent;

            //delBtn.IsEnabled = sel != null;
            //resetBtn.IsEnabled = sel != null;
            //saveBtn.IsEnabled = sel != null;

            if (sel == null)
            {
                ClearInputs();
                setButtonState(OpState.NoSel);
                return;
            }

            setButtonState(OpState.ValidSel);

            txt0.Content = sel.EventName;
            txt1.Text = sel.Date == null ? "" : sel.Date.DateString;
            txt2.Text = sel.Place;
            txt3.Text = sel.Description;
            txt4.Text = sel.Address == null ? "" : sel.Address.AddressLine;

        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {
            setButtonState(OpState.AddClick);

            DisplayGrid.SelectedItem = null;

            // TODO show the event picker combobox

            txt0.Visibility = Visibility.Collapsed;
            txt1.Text = "new date";
            txt2.Text = "new place";
            txt3.Text = "new description";
            txt4.Text = "new address";
        }

        private void delBtn_Click(object sender, RoutedEventArgs e)
        {
            // delete the current event/fact from the person (with confirmation)
        }

        private void resetBtn_Click(object sender, RoutedEventArgs e)
        {
            // when adding, clear all edits. when editing, reset all edits to original values
        }

        private void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            // save all edits
        }

        private void ClearInputs()
        {
            txt0.Content = "";
            txt1.Text = "";
            txt2.Text = "";
            txt3.Text = "";
            txt4.Text = "";
        }

        private enum OpState
        {
            NoSel,
            AddClick,
            ValidSel
        };

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
