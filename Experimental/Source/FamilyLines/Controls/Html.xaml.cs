/*
 * Family.Show derived code provided under MS-PL license.
 * 
 */
using System;
using System.IO;
using System.Windows;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for Html.xaml
    /// </summary>
    public partial class Html
    {

        #region fields

        public int minYear = DateTime.Now.Year; // TODO should be a property

        #endregion

        public Html()
        {
            InitializeComponent();
            searchfield.SelectedIndex = 0;  //set name as default filter
            Option1.IsChecked = true;  //set the default choice to be All people
        }

        #region routed events

        public static readonly RoutedEvent CancelButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CancelButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Html));

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

        private void Ancestors_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Option4.IsChecked = true;
        }

        private void Descendants_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Option4.IsChecked = true;
        }

        private void searchfield_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Option5.IsChecked = true;
        }

        private void searchtext_TextChanged(object sender, RoutedEventArgs e)
        {
            Option5.IsChecked = true; 
        }

        private void Option6_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SourcesHtml.IsEnabled = Option6.IsChecked != true;
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
                if (Option3.IsChecked == true)
                    choice = "3";
                if (Option4.IsChecked == true)
                    choice = "4";
                if (Option5.IsChecked == true)
                    choice = "5";
                if (Option6.IsChecked == true)
                    choice = "6";
                return choice;
            }
        }

        private bool Privacy()
        {
            return PrivacyHtml.IsChecked == true;
        }

        private bool Sources()
        {
            return SourcesHtml.IsChecked == true;
        }

        private decimal Ancestors()
        {
            return Convert.ToDecimal(AncestorsComboBox.Text);
        }

        private decimal Descendants()
        {
            return Convert.ToDecimal(DescendantsComboBox.Text);
        }

        private string searchtextvalue()
        {
            return searchtext.Text;
        }

        private string searchfieldvalue()
        {
            return searchfield.Text;
        }

        private int searchfieldindex()
        {
            return searchfield.SelectedIndex;
        }

        private void Clear()
        {
            DescendantsComboBox.SelectedIndex = 0;
            AncestorsComboBox.SelectedIndex = 0;
            searchfield.SelectedIndex = 0;
            searchtext.Clear();
            PrivacyHtml.IsChecked = false;
            SourcesHtml.IsChecked = false;
            Option1.IsChecked = true;
        }

        private void Export()
        {
            if (Options() == "0") 
                return;

            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.htmlFiles, Properties.Resources.htmlExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaulthtmlExtension;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
                return;

            HtmlExport html = new HtmlExport();

            People familyCollection = App.FamilyCollection;
            PeopleCollection family = App.Family;
            SourceCollection source = App.Sources;
            RepositoryCollection repository = App.Repositories;

            switch (Options())
            {
                case "1":
                    html.ExportAll(family, source, repository, dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), Sources());  //Export the all individuals
                    break;
                case "2":
                    html.ExportCurrent(family, source, repository, dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), Sources());
                    break;
                case "3":
                    html.ExportDirect(family, source, repository, dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), Sources());     //Export current person and immediate family relatives 
                    break;
                case "4":
                    html.ExportGenerations(family, source, repository, Ancestors(), Descendants(), dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), Sources());
                    break;
                case "5":
                    html.ExportFilter(family, source, repository, searchtextvalue(), searchfieldvalue(), searchfieldindex(), dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), Sources());
                    break;
                case "6":
                    int start = minYear;
                    int end = DateTime.Now.Year;
                    html.ExportEventsByDecade(family, source, repository, dialog.FileName, Path.GetFileName(familyCollection.FullyQualifiedFilename), Privacy(), start, end);
                    break;
            }
            MessageBoxResult result = MessageBox.Show(Properties.Resources.SourcesExportMessage, Properties.Resources.ExportResult, MessageBoxButton.YesNo, MessageBoxImage.Question);

            try
            {
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dialog.FileName);
            }
            catch { }
        }

        #endregion

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent)); // TODO this is unintuitive: why raise a 'cancel'?
            ExportKBR();
            Clear(); // TODO why are we clearing the settings? shouldn't they be left alone?
        }

        private void ExportKBR()
        {
            if (Options() == "0")
                return;

            var dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.htmlFiles, Properties.Resources.htmlExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaulthtmlExtension;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
                return;

            PeopleReport pr = new PeopleReport(dialog.FileName, App.Family, App.Sources, App.Repositories);
            pr.Privacy = Privacy();
            pr.generateReport(showHide:true);

            MessageBoxResult result = MessageBox.Show(Properties.Resources.SourcesExportMessage, Properties.Resources.ExportResult, MessageBoxButton.YesNo, MessageBoxImage.Question);

            try
            {
                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(dialog.FileName);
            }
            catch { }
        }
    }
}