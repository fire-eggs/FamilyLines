using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
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
                OnPropertyChanged("PName");
                OnPropertyChanged("Facts");
            }
        }

        public string PName
        {
            get { return Target == null ? "" : Target.FullName; }
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

    }
}
