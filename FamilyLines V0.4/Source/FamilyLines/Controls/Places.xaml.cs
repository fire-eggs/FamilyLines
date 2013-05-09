using System.Windows;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for Places.xaml
    /// </summary>
    public partial class Places
    {
        public Places()
        {
            InitializeComponent();
            Option1.IsChecked = true;  //set the default choice to be All people
        }

        #region routed events

        public static readonly RoutedEvent CancelButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CancelButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Places));

        // Expose this event for this control's container
        public event RoutedEventHandler CancelButtonClick
        {
            add { AddHandler(CancelButtonClickEvent, value); }
            remove { RemoveHandler(CancelButtonClickEvent, value); }
        }

        #endregion

        #region methods

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
            Export();
            Clear();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
            Clear();
        }

        private void Lifetimes_CheckedChanged(object sender, RoutedEventArgs e)
        {
                BirthsCheckBox.IsEnabled = Lifetimes.IsChecked != true;
                DeathsCheckBox.IsEnabled = Lifetimes.IsChecked != true;
                MarriagesCheckBox.IsEnabled = Lifetimes.IsChecked != true;
                CremationsCheckBox.IsEnabled = Lifetimes.IsChecked != true;
                BurialsCheckBox.IsEnabled = Lifetimes.IsChecked != true;
                DivorcesCheckBox.IsEnabled = Lifetimes.IsChecked != true;
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Get the selected options
        /// </summary>
        private string Options()
        {
            {
                string choice = "0";
                if (Option1.IsChecked == true)
                    choice = "1";
                if (Option2.IsChecked == true)
                    choice = "2";
                if (Lifetimes.IsChecked == true)
                    choice = "3";
                return choice;
            }
        }

        private bool Privacy()
        {
            return PrivacyPlaces.IsChecked == true;
        }

        private bool Births()
        {
            return BirthsCheckBox.IsChecked == true;
        }

        private bool Deaths()
        {
            return DeathsCheckBox.IsChecked == true;
        }

        private bool Divorces()
        {
            return DivorcesCheckBox.IsChecked == true;
        }

        private bool Marriages()
        {
            return MarriagesCheckBox.IsChecked == true;
        }

        private bool Burials()
        {
            return BurialsCheckBox.IsChecked == true;
        }

        private bool Cremations()
        {
            return CremationsCheckBox.IsChecked == true;
        }
       
        private void Clear()
        {    
            PrivacyPlaces.IsChecked = false;
            Option1.IsChecked = true;

            BirthsCheckBox.IsEnabled = true;
            DeathsCheckBox.IsEnabled = true;
            MarriagesCheckBox.IsEnabled = true;
            DivorcesCheckBox.IsEnabled = true;
            CremationsCheckBox.IsEnabled = true;
            BurialsCheckBox.IsEnabled = true;
        }

        private void Export()
        {
            if (Options() == "0")
                return; //only run if cancel not clicked

            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.kmlFiles, Properties.Resources.kmlExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaultkmlExtension;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
            {
                //return without doing anything if no file name is input
            }
            else
            {
                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    PlacesExport places = new PlacesExport();

                    string filename = dialog.FileName;

                    string[] summary = null;

                    if (Options() == "1")
                        summary = places.ExportPlaces(App.Family, filename, Privacy(), false, false, true, Burials(), Deaths(), Cremations(), Births(), Marriages(), Divorces());
                    if (Options() == "2")
                        summary = places.ExportPlaces(App.Family, filename, Privacy(), true, false, false, Burials(), Deaths(), Cremations(), Births(), Marriages(), Divorces());
                    if (Options() == "3")
                        summary = places.ExportPlaces(App.Family, filename, Privacy(), false, true, false, Burials(), Deaths(), Cremations(), Births(), Marriages(), Divorces());

                    if (summary[1] == "No file")
                    {
                        MessageBoxResult result = MessageBox.Show(summary[0],
                                                                  Properties.Resources.ExportResult, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBoxResult result = MessageBox.Show(summary[0] + "\n\n" + Properties.Resources.PlacesMessage,
                                                                  Properties.Resources.ExportResult, MessageBoxButton.YesNo, MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                System.Diagnostics.Process.Start(summary[1]);
                            }
                            catch
                            {
                                //no viewer or other error
                            }
                        }
                    } 
                }
            }
        }

        #endregion

    }
}