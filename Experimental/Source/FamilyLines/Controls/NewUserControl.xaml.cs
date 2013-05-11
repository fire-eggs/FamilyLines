using System;
using System.Windows;
using GEDCOM.Net;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for NewUserControl.xaml
    /// </summary>

    public partial class NewUserControl
    {
        public NewUserControl()
        {
            InitializeComponent();

            SetDefaultFocus();
        }

        #region routed events

        public static readonly RoutedEvent AddButtonClickEvent = EventManager.RegisterRoutedEvent(
            "AddButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewUserControl));
        
        public static readonly RoutedEvent CancelButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CancelButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NewUserControl));

        // Expose this event for this control's container
        public event RoutedEventHandler AddButtonClick
        {
            add { AddHandler(AddButtonClickEvent, value); }
            remove { RemoveHandler(AddButtonClickEvent, value); }
        }

        public event RoutedEventHandler CancelButtonClick
        {
            add { AddHandler(CancelButtonClickEvent, value); }
            remove { RemoveHandler(CancelButtonClickEvent, value); }
        }

        #endregion

        #region event handlers

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a new person with the specified inputs
            Person newPerson = new Person(NamesInputTextBox.Text, SurnameInputTextBox.Text);

            // TODO need a gender dropdown (?) - support other gender values
            // Setup the properties based on the input
            newPerson.Gender = (MaleRadioButton.IsChecked == true) ? Gender.Male : Gender.Female;
            newPerson.BirthPlace = BirthPlaceInputTextBox.Text;
            newPerson.IsLiving = true;

            DateTime birthdate = App.StringToDate(BirthDateInputTextBox.Text);
            if (birthdate != DateTime.MinValue)
                newPerson.BirthDate = birthdate;

            App.Family.Current = newPerson;
            App.Family.Add(newPerson);
            App.Family.OnContentChanged();

            RaiseEvent(new RoutedEventArgs(AddButtonClickEvent));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
        }

        #endregion

        #region helper methods

        public void SetDefaultFocus()
        {
            // Set the focus to the first name textbox for quick entry
            NamesInputTextBox.Focus();
        }

        /// <summary>
        /// Clear the input fields
        /// </summary>
        public void ClearInputFields()
        {
            NamesInputTextBox.Clear();
            SurnameInputTextBox.Clear();
            BirthDateInputTextBox.Clear();
            BirthPlaceInputTextBox.Clear();
            MaleRadioButton.IsChecked = true;
        }

        #endregion

    }
}