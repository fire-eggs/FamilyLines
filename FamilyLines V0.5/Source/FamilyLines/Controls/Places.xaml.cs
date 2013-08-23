/*
 * Family.Show derived code provided under MS-PL license.
 */
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
        /// Get export type
        /// </summary>
        /// <returns></returns>
        private ExportType GetExportType()
        {
            if (Lifetimes.IsChecked == true)
                return ExportType.Lifetimes;
            if (Option2.IsChecked == true)
                return ExportType.PlacesWithTimes;
            return ExportType.Places;
        }

        /// <summary>
        /// Get export options
        /// </summary>
        /// <returns></returns>
        private ExportOptions GetExportOptions()
        {
            var options = ExportOptions.None;
            if (Privacy())
                options |= ExportOptions.HideLivingPeople;
            if (Births())
                options |= ExportOptions.Births;
            if (Deaths())
                options |= ExportOptions.Deaths;
            if (Burials())
                options |= ExportOptions.Burials;
            if (Cremations())
                options |= ExportOptions.Cremations;
            if (Marriages())
                options |= ExportOptions.Marriages;
            if (Divorces())
                options |= ExportOptions.Divorces;
            return options;
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
            if (GetExportOptions() == ExportOptions.None)
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

                    summary = PlacesExport.ExportPlaces(App.Family, filename, GetExportType(), GetExportOptions());

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