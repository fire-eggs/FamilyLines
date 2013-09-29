/*
 * Family.Show derived code provided under MS-PL license.
 */
using System;
using System.IO;
using System.IO.Packaging;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using GEDCOM.Net;
using KBS.FamilyLines.Controls;
using KBS.FamilyLinesLib;
using SUT.PrintEngine.Paginators;
using SUT.PrintEngine.Utils;
using VisualPrint;

namespace KBS.FamilyLines
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        #region fields

        // The list of people, sources and repositories. This is a global list shared by the application.
        People familyCollection = App.FamilyCollection;
        PeopleCollection family = App.Family;
        SourceCollection source = App.Sources;
        RepositoryCollection repository = App.Repositories;

        bool hideDiagramControls = false;
        private Properties.Settings appSettings = Properties.Settings.Default;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            Title = Properties.Resources.ApplicationName;

            BuildOpenMenu();
            BuildThemesMenu();
            BuildShadowsMenu();
            family.CurrentChanged += People_CurrentChanged;
            ProcessCommandLines();

            AddChildH += Execute_AddChild;
            AddParentH += Execute_AddParent;
            AddSpouseH += Execute_AddSpouse;
            EditMarrH += Execute_EditMarriage;
            EditAllDetailsH += Execute_EditAllDetails;
            ViewAllFactsH += Execute_ViewAllFacts;
            ViewAllEventsH += Execute_ViewAllEvents;
        }        

        #region event handlers

        /// <summary>
        /// Event handler when the primary person has changed.
        /// </summary>
        private void People_CurrentChanged(object sender, EventArgs e)
        {
            if (family.Current != null)
                DetailsControl.DataContext = family.Current;  
        }

        private void NewUserControl_AddButtonClick(object sender, RoutedEventArgs e)
        {
            HideNewUserControl();
            family.RebuildTrees(); // TODO better place/way to call this?
            ShowDetailsPane();
        }

        private void NewUserControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            HideNewUserControl();
            ShowWelcomeScreen();
            // KBR the process of invoking 'New' wipes the current loaded Family state. So we MUST
            // return to the Welcome Screen at this point.
            //ReturnToWelcomeOrCurrentFamily();
        }

        private void DetailsControl_PersonInfoClick(object sender, RoutedEventArgs e)
        {
            PersonInfoControl.DataContext = family.Current;
            // Uses an animation to show the Person Info Control
            ((Storyboard)Resources["ShowPersonInfo"]).Begin(this);
        }

        private void DetailsControl_FamilyDataClick(object sender, RoutedEventArgs e)
        {
            FamilyDataControl.MakeVisible();
            FamilyDataControl.Refresh();
            // Uses an animation to show the Family Data Control
            ((Storyboard)Resources["ShowFamilyData"]).Begin(this);
        }

        /// <summary>
        /// Event handler when all people in the people collection have been deleted.
        /// </summary>
        private void DetailsControl_EveryoneDeleted(object sender, RoutedEventArgs e)
        {
            // Everyone was deleted show the create new user control
            NewFamily(sender, e);
        }

        private void PersonInfoControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            // Uses an animation to hide the Person Info Control
            ((Storyboard)this.Resources["HidePersonInfo"]).Begin(this);
        }

        private void FamilyDataControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            // Uses an animation to hide the Family Data Control
            HideFamilyDataControl();
        }

        private void ShowPersonInfo_StoryboardCompleted(object sender, EventArgs e)
        {
            disableButtons();
            PersonInfoControl.SetDefaultFocus();
        }

        private void HidePersonInfo_StoryboardCompleted(object sender, EventArgs e)
        {
            family.OnContentChanged();
            DetailsControl.SetDefaultFocus();
            enableButtons();
        }

        private void ShowFamilyData_StoryboardCompleted(object sender, EventArgs e)
        {
            disableButtons();
            FamilyDataControl.SetDefaultFocus();
        }

        private void HideFamilyData_StoryboardCompleted(object sender, EventArgs e)
        {
            DetailsControl.SetDefaultFocus();
            enableButtons();
        }

        private void WelcomeUserControl_NewButtonClick(object sender, RoutedEventArgs e)
        {
            NewFamily(sender, e);
        }

        private void WelcomeUserControl_OpenButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFamily(sender, e);
        }

        private void WelcomeUserControl_ImportButtonClick(object sender, RoutedEventArgs e)
        {
            ImportGedcom(sender, e);
        }

        private void AboutControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            AboutControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void LanguageControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            LanguageControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void StatisticsControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            StatisticsControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void PhotoViewerControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            PhotoViewerControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void AttachmentViewerControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            AttachmentViewerControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void StoryViewerControl_CloseButtonClick(object sender, RoutedEventArgs e)
        {
            StoryViewerControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void MergeControl_DoneButtonClick(object sender, RoutedEventArgs e)
        {
            MergeControl.Visibility = Visibility.Hidden;
            
            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
            dialog.Title = Properties.Resources.SaveAs;
            dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
            dialog.ShowSave();
            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                TaskBar.Current.Loading();
                familyCollection.Save(dialog.FileName);
                // Remove the file from its current position and add it back to the top/most recent position.
                App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                BuildOpenMenu();
            }
            else
                familyCollection.FullyQualifiedFilename = "";

            family.OnContentChanged();
            UpdateStatus();
            TaskBar.Current.Restore();
            removeControlFocus();
        }

        private void SaveControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            SaveControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            SaveControl.Clear();
        }

        private void SaveControl_SaveButtonClick(object sender, RoutedEventArgs e)
        {
            SaveControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            SaveFamilyAs();
            SaveControl.Clear();
        }

        private void GedcomLocalizationControl_ContinueButtonClick(object sender, RoutedEventArgs e)
        {
            GedcomLocalizationControl.Visibility = Visibility.Hidden;
            appSettings.EnableUTF8 = GedcomLocalizationControl.EnableUTF8CheckBox.IsChecked == true;
            appSettings.Save();
            ImportGedcom();
        }

        private void GedcomLocalizationControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            GedcomLocalizationControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            ReturnToWelcomeOrCurrentFamily();
        }

        /// <summary>
        /// User has cancelled a New or Import dialog. Return to an already loaded family if we
        /// have one, otherwise show the 'Welcome' dialog.
        /// </summary>
        private void ReturnToWelcomeOrCurrentFamily()
        {
            //ShowWelcomeScreen();
            CollapseDetailsPanels();
            ShowDetailsPane();
            family.OnContentChanged();
            TheFamilyView.Init(); // TODO this is brute force (Family disconnect?)

            // The collection requires a primary-person, use the first
            // person added to the collection as the primary-person.
            if (family.Count > 0)
                family.Current = family[0];

            TaskBar.Current.Restore();
            UpdateStatus();
            App.canExecuteJumpList = true;

            if (family.Count == 0)
            {
                ShowWelcomeScreen();
                UpdateStatus();
            }
        }

        private void SourcesControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            SourcesControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void DateCalculatorControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            DateCalculatorControl.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void RepositoriesControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            RepositoriesControl.Visibility = Visibility.Hidden;
            removeControlFocus();       
        }

        private void HtmlControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            HtmlControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            UpdateStatus();
        }

        private void PlacesControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            PlacesControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            UpdateStatus();
        }

        private void ExtractControl_CancelButtonClick(object sender, RoutedEventArgs e)
        {
            ExtractControl.Visibility = Visibility.Hidden;
            removeControlFocus();
            UpdateStatus();
        }

        private void WelcomeUserControl_OpenRecentFileButtonClick(object sender, RoutedEventArgs e)
        {
            Button item = (Button)e.OriginalSource;
            string file = item.CommandParameter as string;

            if (!string.IsNullOrEmpty(file))
            {
                // Load the selected family file
                
                bool fileLoaded = LoadFamily(file);

                if (fileLoaded)
                {
                    ShowDetailsPane();
                    // This will tell the diagram to redraw and the details panel to update.
                    family.OnContentChanged();
                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(file);
                    App.RecentFiles.Insert(0, file);
                    BuildOpenMenu();
                    family.IsDirty = false;
                    UpdateStatus();
                }
                else
                {
                    Title = Properties.Resources.FamilyShow;
                }
            }
        }

        public event RoutedEventHandler AddChildH
        {
            add
            {
                AddHandler(Commands.AddChild, value);
            }
            remove
            {
                RemoveHandler(Commands.AddChild, value);
            }
        }
        public event RoutedEventHandler AddParentH
        {
            add
            {
                AddHandler(Commands.AddParent, value);
            }
            remove
            {
                RemoveHandler(Commands.AddParent, value);
            }
        }
        public event RoutedEventHandler AddSpouseH
        {
            add
            {
                AddHandler(Commands.AddSpouse, value);
            }
            remove
            {
                RemoveHandler(Commands.AddSpouse, value);
            }
        }
        public event RoutedEventHandler EditMarrH
        {
            add
            {
                AddHandler(Commands.EditMarriage, value);
            }
            remove
            {
                RemoveHandler(Commands.EditMarriage, value);
            }
        }

        public event RoutedEventHandler EditAllDetailsH
        {
            add
            {
                AddHandler(Commands.EditAllFactDetails, value);
            }
            remove
            {
                RemoveHandler(Commands.EditAllFactDetails, value);
            }
        }

        public event RoutedEventHandler ViewAllFactsH
        {
            add
            {
                AddHandler(Commands.ViewAllFacts, value);
            }
            remove
            {
                RemoveHandler(Commands.ViewAllFacts, value);
            }
        }

        public event RoutedEventHandler ViewAllEventsH
        {
            add
            {
                AddHandler(Commands.ViewAllEvents, value);
            }
            remove
            {
                RemoveHandler(Commands.ViewAllEvents, value);
            }
        }

        #endregion

        #region menu command handlers

        #region new menu

        private void NewFamily(object sender, RoutedEventArgs e)
        {
            NewFamily();
        }

        #endregion

        #region open menu

        private void OpenFamily(object sender, RoutedEventArgs e)
        {
            OpenFamily();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ImportGedcom(object sender, EventArgs e)
        {
            App.canExecuteJumpList = false;
            removeControlFocus();
            GedcomLocalizationImport();
        }

        private void Merge(object sender, RoutedEventArgs e)
        {

            App.canExecuteJumpList = false;

            string oldFilePath = string.Empty;

            #region prompt to save before merging

            if (family.IsDirty)
            {
                MessageBoxResult result = MessageBox.Show(Properties.Resources.SaveBeforeMerge, Properties.Resources.Save, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                if(result == MessageBoxResult.Yes)
                {

                    if (!string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
                    {
                        oldFilePath = familyCollection.FullyQualifiedFilename;
                        familyCollection.Save(familyCollection.FullyQualifiedFilename);
                    }
                    else
                    {

                        CommonDialog dialog = new CommonDialog();
                        dialog.InitialDirectory = People.ApplicationFolderPath;
                        dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
                        dialog.Title = Properties.Resources.SaveAs;
                        dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                        dialog.ShowSave();

                        if (!string.IsNullOrEmpty(dialog.FileName))
                        {
                            oldFilePath = dialog.FileName;
                            familyCollection.Save(dialog.FileName);
                        }
                    }


                }

                if (result == MessageBoxResult.Cancel)
                {
                    App.canExecuteJumpList = true;
                    return;
                }
            }

            #endregion

            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.MergingStatus;  //Update status bar

            CommonDialog mergedialog = new CommonDialog();
            mergedialog.InitialDirectory = People.ApplicationFolderPath;
            mergedialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
            mergedialog.Title = Properties.Resources.Merge;
            mergedialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
            mergedialog.ShowOpen();

            if (!string.IsNullOrEmpty(mergedialog.FileName))
            {

                Title = Properties.Resources.FamilyShow;
                string[,] summary = MergeFamily(mergedialog.FileName);

                if (summary != null)
                {
                    try
                    {
                        giveControlFocus();

                        MergeControl.summary = summary;

                        if (App.FamilyCollection.ExistingPeopleCollection != null && App.FamilyCollection.DuplicatePeopleCollection != null)
                        {
                            if (App.FamilyCollection.ExistingPeopleCollection.Count > 0 && App.FamilyCollection.DuplicatePeopleCollection.Count > 0)
                            {
                                MergeControl.Visibility = Visibility.Visible;
                                MergeControl.ShowMergeSummary();
                            }
                            else
                            {
                                MergeControl.Visibility = Visibility.Visible;
                                MergeControl.summary = summary;
                                MergeControl.ShowSummary();
                            }
                        }

                        else
                        {
                            MergeControl.Visibility = Visibility.Visible;
                            MergeControl.summary = summary;
                            MergeControl.ShowSummary();
                        }

                        family.RebuildTrees(); // tree data needs updating
                        family.OnContentChanged();
                    }
                    catch 
                    {
                        MergeControl.Visibility = Visibility.Hidden;
                        MessageBox.Show(Properties.Resources.MergeExistingError, Properties.Resources.Merge, MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowWelcomeScreen();
                        UpdateStatus();  
                    }

                }
                else
                {
                    //if the merge fails, reload the original file and continue. Prompt the user.
                    if (LoadFamily(oldFilePath))
                        MessageBox.Show(Properties.Resources.MergeFailed1, Properties.Resources.Merge, MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                    {
                        MessageBox.Show(Properties.Resources.MergeFailed2, Properties.Resources.Merge, MessageBoxButton.OK, MessageBoxImage.Error);
                        ShowWelcomeScreen();
                        UpdateStatus();
                    }
                }  
            }
            else
               UpdateStatus();
        }

        private void OpenRecentFile(object sender, RoutedEventArgs e)
        {
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.LoadingStatus;
            MenuItem item = (MenuItem)sender;
            string file = item.CommandParameter as string;

            if (!string.IsNullOrEmpty(file))
            {
                PromptToSave();
                LoadFamily(file);
                ShowDetailsPane();
                // This will tell the diagram to redraw and the details panel to update.
                family.OnContentChanged();
                // Remove the file from its current position and add it back to the top/most recent position.
                App.RecentFiles.Remove(file);
                App.RecentFiles.Insert(0, file);
                BuildOpenMenu();
                family.IsDirty = false;
            }
            UpdateStatus();
            e.Handled = true;
        }
        
        private void ClearRecentFiles(object sender, RoutedEventArgs e)
        {
            App.RecentFiles.Clear();
            App.SaveRecentFiles();
            BuildOpenMenu();
        }

        #endregion 

        #region save menu

        private void SaveFamily(object sender, RoutedEventArgs e)
        {
            App.canExecuteJumpList = false;

            Title = Title = Properties.Resources.FamilyShow + " " + Properties.Resources.SavingStatus;  //Update status bar
            // Prompt to save if the file has not been saved before, otherwise just save to the existing file.
            if (string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
            {
                CommonDialog dialog = new CommonDialog();
                dialog.InitialDirectory = People.ApplicationFolderPath;
                dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
                dialog.Title = Properties.Resources.SaveAs;
                dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                dialog.ShowSave();

                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    TaskBar.Current.Loading();
                    familyCollection.Save(dialog.FileName);
                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                    App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                    BuildOpenMenu();
                }
            }
            else
            {
                TaskBar.Current.Loading();
                familyCollection.Save(false);
                // Remove the file from its current position and add it back to the top/most recent position.
                App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                BuildOpenMenu();
            }
            App.canExecuteJumpList = true;
            TaskBar.Current.Restore();
            UpdateStatus();
        }

        private void SaveFamilyAs(object sender, RoutedEventArgs e)
        {
            giveControlFocus();
            SaveControl.Visibility = Visibility.Visible;
        }

        private void ExportGedcom(object sender, EventArgs e)
        {
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.ExportingStatus;
            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.GedcomFiles, Properties.Resources.GedcomExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaultGedcomExtension;
            dialog.ShowSave();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                GedcomExport ged = new GedcomExport();
                try
                {
                    ged.Export(familyCollection.ExportHeader, family, source, repository, 
                               dialog.FileName, familyCollection.FullyQualifiedFilename, 
                               Properties.Resources.Language);
                    MessageBox.Show(this, Properties.Resources.GedcomExportSucessfulMessage,
                        Properties.Resources.Export, MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch
                {
                    MessageBox.Show(this, Properties.Resources.GedcomExportFailedMessage,
                        Properties.Resources.Export, MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }

            UpdateStatus();
        }

        private void ExportHtml(object sender, RoutedEventArgs e)
        {
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.ExportingStatus;
            giveControlFocus();
            HtmlControl.Visibility = Visibility.Visible;
            HtmlControl.minYear = (int)DiagramControl.TimeSlider.Minimum;
        }

        private void ExportPlaces(object sender, RoutedEventArgs e)
        {
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.ExportingStatus;
            giveControlFocus();
            PlacesControl.Visibility = Visibility.Visible;
        }

        private void ExtractFiles(object sender, RoutedEventArgs e)
        {
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.ExtractingStatus;
            giveControlFocus();
            ExtractControl.Visibility = Visibility.Visible;
        }

        #endregion

        #region tools menu

        private void EditRepositories(object sender, RoutedEventArgs e)
        {
            giveControlFocus();
            RepositoriesControl.Visibility = Visibility.Visible;
            RepositoriesControl.RepositoriesCombobox.ItemsSource = repository;
        }

        private void EditSources(object sender, RoutedEventArgs e)
        {
            giveControlFocus();
			// KBR changes for missing sources
            SourcesControl.Init();
            SourcesControl.Visibility = Visibility.Visible;
//            SourcesControl.SourcesCombobox.ItemsSource = source;
        }

        private void Statistics(object sender, EventArgs e)
        {
            giveControlFocus();
            StatisticsControl.Visibility = Visibility.Visible;
            StatisticsControl.DisplayStats(family, source, repository);
        }

        private void Photos(object sender, EventArgs e)
        {
            giveControlFocus();
            enableMenus();
            PhotoViewerControl.Visibility = Visibility.Visible; 
            PhotoViewerControl.LoadPhotos(family);

        }

        private void Dates(object sender, EventArgs e)
        {
            giveControlFocus();
            enableMenus();
            DateCalculatorControl.Visibility = Visibility.Visible;
        }

        private void Attachments(object sender, EventArgs e)
        {
            giveControlFocus();
            enableMenus();
            AttachmentViewerControl.Visibility = Visibility.Visible;
            AttachmentViewerControl.LoadAttachments(family);
        }

        private void Storys(object sender, EventArgs e)
        {
            giveControlFocus();
            enableMenus();
            StoryViewerControl.Visibility = Visibility.Visible;
        }


        #endregion

        #region print menu

        private void PrintSUT(object sender, RoutedEventArgs e)
        {
            // Print the full chart with the SUT WPF Print Engine
            // http://www.codeproject.com/Articles/238135/WPF-Print-Engine-Part-I

            var visual = DiagramControl.Diagram;
            var visualSize = new Size(visual.ActualWidth * visual.Scale,
                                      (visual.ActualHeight + 100) * visual.Scale);
            var printControl = PrintControlFactory.Create(visualSize, visual);

//            var printControl = PrintControlFactory.Create(visual);
            printControl.ShowPrintPreview();
        }

        private FlowDocument _document;

        private void OnPrint(object sender, RoutedEventArgs e)
        {
            //FlowDocument document = (e.Source as PreviewWindow).Document;
            //if (document == null)
            //    return;
            DocumentPaginator paginator = ((IDocumentPaginatorSource)_document).DocumentPaginator;
            new PrintDialog().PrintDocument(paginator, "Printing");
        }

        private void PrintSUT2(object sender, RoutedEventArgs e)
        {

            // Print the full chart with Fred Song's VisualPrint
            // http://www.codeproject.com/Articles/164033/WPF-Visual-Print-Component

            var visual = DiagramControl.Diagram;

            _document = PrintHelper.CreateFlowDocument(visual, new Size(816,1248));

            PreviewWindow win = new PreviewWindow(_document);
            win.Print += new RoutedEventHandler(OnPrint);
            win.Owner = this;
            win.ShowDialog();
        }

        private void PrintKBR3(object sender, RoutedEventArgs e)
        {
            PrintHelper.DoPreview("foo", DiagramControl.Diagram);
        }

        private void PrintKBR2(object sender, RoutedEventArgs e)
        {
            // DiagramBorder - prints only the current view.

            DiagramControl.ZoomSliderPanel.Visibility = Visibility.Hidden;
            DiagramControl.TimeSliderPanel.Visibility = Visibility.Hidden;

            PrintHelper.PrintPreview(this, DiagramBorder);

            if (!hideDiagramControls)
            {
                DiagramControl.ZoomSliderPanel.Visibility = Visibility.Visible;
                DiagramControl.TimeSliderPanel.Visibility = Visibility.Visible;
            }

        }

        private void PrintKBR(object sender, RoutedEventArgs e)
        {
            // DiagramControl.Diagram - prints the full tree

            PrintHelper.PrintPreview(this, DiagramControl.Diagram);

#if false
            string path = "test.xps";

            Package package = Package.Open(path, FileMode.Create);
            XpsDocument document = new XpsDocument(package);
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(document);
            FixedDocument doc = new FixedDocument();

            FixedPage page1 = new FixedPage();
            PageContent page1Content = new PageContent();
            ((System.Windows.Markup.IAddChild)page1Content).AddChild(page1);

            // KBR this doesn't work as the diagram is already a child of something else
            page1.Children.Add(DiagramControl.Diagram);

            doc.Pages.Add(page1Content);
            writer.Write(doc);
            document.Close();
            package.Close();
#endif

#if false
            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.XpsFiles, Properties.Resources.XpsExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaultXpsExtension;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
                return;

            try
            {
                // Create the XPS document from the window's main container (in this case, a grid) 
                Package package = Package.Open(dialog.FileName, FileMode.Create);
                XpsDocument xpsDoc = new XpsDocument(package);
                XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

                // Hide the zoom control and time control before the diagram is saved
                DiagramControl.ZoomSliderPanel.Visibility = Visibility.Hidden;
                DiagramControl.TimeSliderPanel.Visibility = Visibility.Hidden;

                // Since DiagramBorder derives from FrameworkElement, the XpsDocument writer knows
                // how to output it's contents. The border is used instead of the DiagramControl
                // so that the diagram background is output as well as the digram control itself.

                xpsWriter.Write(DiagramControl.Diagram);
                xpsDoc.Close();
                package.Close();
            }

            catch
            {
                //save as xps fails if saving as an existing file which is open.
            }

            // Show the zoom control and time control again
            if (hideDiagramControls == false)
            {
                DiagramControl.ZoomSliderPanel.Visibility = Visibility.Visible;
                DiagramControl.TimeSliderPanel.Visibility = Visibility.Visible;
            }
#endif

            //var pd = new PrintDialog();
            //if (!pd.ShowDialog().GetValueOrDefault())
            //    return;

            //pd.PrintVisual(DiagramControl.Diagram,"blah");

        }

        private void Print(object sender, RoutedEventArgs e)
        {
                PrintDialog pd = new PrintDialog();

                if ((bool)pd.ShowDialog().GetValueOrDefault())
                {
                    // Hide the zoom control and time control before the diagram is saved
                    DiagramControl.ZoomSliderPanel.Visibility = Visibility.Hidden;
                    DiagramControl.TimeSliderPanel.Visibility = Visibility.Hidden;

                    //Make a stackpanel to hold the contents.
                    StackPanel pageArea = new StackPanel();

                    double padding = 20;
                    double titleheight = 25;
                    double heightActual = 0;
                    double widthActual = 0;

                    //Diagram
                    VisualBrush diagramFill = new VisualBrush();
                    System.Windows.Shapes.Rectangle diagram = new System.Windows.Shapes.Rectangle();

                    //Print background when black theme is used because diagram has white text
                    if (appSettings.Theme == @"Themes\Black\BlackResources.xaml")
                    {
                        heightActual = this.DiagramBorder.ActualHeight;
                        widthActual = this.DiagramBorder.ActualWidth;
                        diagramFill = new VisualBrush(DiagramBorder);
                        diagram.Margin = new Thickness(0, 0, 0, 0);
                        diagram.Fill = diagramFill;
                    }
                    else
                    {
                        heightActual = this.DiagramBorder.ActualHeight;
                        widthActual = this.DiagramBorder.ActualWidth;
                        diagramFill = new VisualBrush(DiagramControl);
                        diagram.Stroke = Brushes.Black;
                        diagram.StrokeThickness = 0.5;
                        diagram.Margin = new Thickness(0, 0, 0, 0);
                        diagram.Fill = diagramFill;
                    }

                    //Titles
                    TextBlock titles = new TextBlock();
                    titles.Height = titleheight;
                    titles.Text = string.Format(Properties.Resources.ReportHeader, App.Family.Current.FullName, DiagramControl.YearFilter.Content);

                    //Scale
                    double scale = Math.Min((pd.PrintableAreaWidth - padding - padding) / widthActual, (pd.PrintableAreaHeight - padding - padding - titleheight) / heightActual);

                    diagram.Width = scale * widthActual;
                    diagram.Height = scale * heightActual;

                    //Page Area
                    pageArea.Margin = new Thickness(padding);
                    pageArea.Children.Add(titles);
                    pageArea.Children.Add(diagram);
                    pageArea.Measure(new Size(pd.PrintableAreaWidth, pd.PrintableAreaHeight));
                    pageArea.Arrange(new Rect(new Point(0, 0), pageArea.DesiredSize));

                    pd.PrintVisual(pageArea, App.Family.Current.FullName);
       
                    // Show the zoom control and time control again
                    if (hideDiagramControls == false)
                    {
                        DiagramControl.ZoomSliderPanel.Visibility = Visibility.Visible;
                        DiagramControl.TimeSliderPanel.Visibility = Visibility.Visible;
                    }
                }
        }

        private void ExportXps(object sender, EventArgs e)
        {
            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.XpsFiles, Properties.Resources.XpsExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaultXpsExtension;
            dialog.ShowSave();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                try
                {
                    // Create the XPS document from the window's main container (in this case, a grid) 
                    Package package = Package.Open(dialog.FileName, FileMode.Create);
                    XpsDocument xpsDoc = new XpsDocument(package);
                    XpsDocumentWriter xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);

                    // Hide the zoom control and time control before the diagram is saved
                    DiagramControl.ZoomSliderPanel.Visibility = Visibility.Hidden;
                    DiagramControl.TimeSliderPanel.Visibility = Visibility.Hidden;

                    // Since DiagramBorder derives from FrameworkElement, the XpsDocument writer knows
                    // how to output it's contents. The border is used instead of the DiagramControl
                    // so that the diagram background is output as well as the digram control itself.

                    xpsWriter.Write(DiagramBorder);
                    xpsDoc.Close();
                    package.Close();

                }

                catch
                { 
                //save as xps fails if saving as an existing file which is open.
                }

                // Show the zoom control and time control again
                if (hideDiagramControls == false)
                {
                    DiagramControl.ZoomSliderPanel.Visibility = Visibility.Visible;
                    DiagramControl.TimeSliderPanel.Visibility = Visibility.Visible;
                }
            }
        }

        #endregion

        #region themes menu

        private void ChangeTheme(object sender, RoutedEventArgs e)
        {

            MenuItem item = (MenuItem)sender;
            string theme = item.CommandParameter as string;

            ResourceDictionary rd = new ResourceDictionary();
            rd.MergedDictionaries.Add(Application.LoadComponent(new Uri(theme, UriKind.Relative)) as ResourceDictionary);
            Application.Current.Resources = rd;

            // Save the theme setting
            appSettings.Theme = theme;
            appSettings.Save();

            family.OnContentChanged();
            PersonInfoControl.OnThemeChange();
            WelcomeUserControl.OnThemeChange();
            UpdateStatus();
            this.DiagramControl.TimeSlider.Value = DateTime.Now.Year;

        }

        #endregion

        #region help menu

        private void About(object sender, EventArgs e)
        {
            giveControlFocus();
            AboutControl.Visibility = Visibility.Visible;
        }

        private void Languages(object sender, EventArgs e)
        {
            giveControlFocus();
            LanguageControl.Visibility = Visibility.Visible;
        }

        #endregion

        #region View Menu

        private bool UsingFamilyView;
        private void FamilyViewClick(object sender, RoutedEventArgs e)
        {
            TheFamilyView.Init();

            DiagramPane.Visibility = !UsingFamilyView ? Visibility.Collapsed : Visibility.Visible;
            // Hide/Display DiagramControl, issue 1590.
            DiagramControl.Visibility = !UsingFamilyView ? Visibility.Collapsed : Visibility.Visible;  
            FamilyViewPane.Visibility = !UsingFamilyView ? Visibility.Visible : Visibility.Collapsed;
            TheFamilyView.Visibility = !UsingFamilyView ? Visibility.Visible : Visibility.Collapsed;

            UsingFamilyView = !UsingFamilyView;

            FamilyViewMenuItem.IsChecked = UsingFamilyView;
        }

        #endregion

        #endregion

        #region menu command helper methods

        private void WipeFamily()
        {
            family.CurrentChanged -= People_CurrentChanged;

            family.Current = null;
            family.Clear();
            source.Clear();
            repository.Clear();

            familyCollection.FullyQualifiedFilename = null;
            family.CurrentChanged += People_CurrentChanged;

            family.OnContentChanged();
            family.IsDirty = false;
            ShowNewUserControl();
        }

        private void SaveFamilyOnNew()
        {
            // Prompt to save if the file has not been saved before.
            if (string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
            {
                CommonDialog dialog = new CommonDialog();
                dialog.InitialDirectory = People.ApplicationFolderPath;
                dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
                dialog.Title = Properties.Resources.SaveAs;
                dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                dialog.ShowSave();

                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    familyCollection.Save(dialog.FileName);
                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                    App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                    BuildOpenMenu();
                }
            }

            // Otherwise just save to the existing file.
            else
            {
                familyCollection.Save(false);
                // Remove the file from its current position and add it back to the top/most recent position.
                App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                BuildOpenMenu();
            }
        }

        /// <summary>
        /// Starts a new family.
        /// </summary>
        private void NewFamily()
        {
            giveControlFocus();
            ReleasePhotos();

            // Do not prompt for fully saved or welcome screen new families.
            if (!family.IsDirty || (family.IsDirty && family.Count == 0))
            {
                WipeFamily();
            }
            else
            {
                MessageBoxResult result = MessageBox.Show(Properties.Resources.NotSavedMessage,
                    Properties.Resources.Save, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        SaveFamilyOnNew();
                        WipeFamily();
                        break;
                    case MessageBoxResult.No:
                        WipeFamily();
                        break;
                    case MessageBoxResult.Cancel:
                        removeControlFocus();
                        break;
                }
            }

            UpdateStatus();
        }

        /// <summary>
        /// Opens a familyx file.
        /// </summary>
        private void OpenFamily()
        {
            App.canExecuteJumpList = false;
            bool loaded = true;
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.LoadingStatus;
            PromptToSave();

            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFilesAll, Properties.Resources.FamilyxExtension));
            dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyFiles, Properties.Resources.FamilyExtension));
            dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyShowFiles, Properties.Resources.FamilyShowExtensions));
            dialog.Title = Properties.Resources.Open;
            dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
            dialog.ShowOpen();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                if (Path.GetExtension(dialog.FileName) == Properties.Resources.DefaultFamilyxExtension)
                {
                    loaded = LoadFamily(dialog.FileName);
                }
                else if (Path.GetExtension(dialog.FileName) == Properties.Resources.DefaultFamilyExtension)
                {
                    loaded = LoadVersion2(dialog.FileName);
                }
               
                if (!loaded)
                {
                    ShowWelcomeScreen();
                    UpdateStatus();
                }
                else
                {
                    CollapseDetailsPanels();
                    ShowDetailsPane();
                    family.OnContentChanged();
                }

                TaskBar.Current.Restore();

                // Do not add non default files to recent files list.
                if (familyCollection.FullyQualifiedFilename.EndsWith(Properties.Resources.DefaultFamilyxExtension))
                {
                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                    App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                    BuildOpenMenu();
                    family.IsDirty = false;
                }
                   
            }

            if (family.Count==0)
                ShowWelcomeScreen();

            UpdateStatus();
            App.canExecuteJumpList = true;

        }

        /// <summary>
        /// Saves a family with and prompts for a file name
        /// </summary>
        private void SaveFamilyAs()
        {
            App.canExecuteJumpList = false;

            if (SaveControl.Options() != "0")
            {
                Title = Title = Properties.Resources.FamilyShow + " " + Properties.Resources.SavingStatus;  //Update status bar
                CommonDialog dialog = new CommonDialog();
                dialog.InitialDirectory = People.ApplicationFolderPath;
                dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension)); ;
                dialog.Title = Properties.Resources.SaveAs;
                dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                dialog.ShowSave();

                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    TaskBar.Current.Loading();

                    bool privacy = SaveControl.Privacy();

                    if (SaveControl.Options() == "1")
                        familyCollection.SavePrivacy(dialog.FileName, privacy);
                    if (SaveControl.Options() == "2")
                        familyCollection.SaveCurrent(dialog.FileName, privacy);
                    if (SaveControl.Options() == "3")
                        familyCollection.SaveDirect(dialog.FileName, privacy);
                    if (SaveControl.Options() == "4")
                        familyCollection.SaveGenerations(dialog.FileName, SaveControl.Ancestors(), SaveControl.Descendants(), privacy);     //then save and load the new family
                }
            }

            if (!string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
            {
                if (familyCollection.FullyQualifiedFilename.EndsWith(Properties.Resources.DefaultFamilyxExtension))
                {
                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                    App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                    BuildOpenMenu();
                    family.IsDirty = false;
                }
            }

            family.OnContentChanged();
            UpdateStatus();
            TaskBar.Current.Restore();
            App.canExecuteJumpList = true;
        }

        /// <summary>
        /// Prompts the user to select encoding option for GEDCOM import.
        /// </summary>
        private void GedcomLocalizationImport()
        {
            App.canExecuteJumpList = false;
            Title = Properties.Resources.FamilyShow + " " + Properties.Resources.ImportingStatus;
            PromptToSave();

            giveControlFocus();
            GedcomLocalizationControl.Visibility = Visibility.Visible;
            GedcomLocalizationControl.EnableUTF8CheckBox.IsChecked = appSettings.EnableUTF8;
        }

        /// <summary>
        /// Imports a selected GEDCOM file.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private void ImportGedcom()
        {
            App.canExecuteJumpList = false;
            bool loaded = true;

            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.GedcomFiles, Properties.Resources.GedcomExtension));
            dialog.Title = Properties.Resources.HeaderImport;
            dialog.DefaultExtension = Properties.Resources.DefaultGedcomExtension;
            dialog.ShowOpen();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {

                TaskBar.Current.Loading();
                ReleasePhotos();

                string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App.ApplicationFolderName);
                tempFolder = Path.Combine(tempFolder, FamilyLinesLib.App.AppDataFolderName);

                People.RecreateDirectory(tempFolder);
                People.RecreateDirectory(Path.Combine(tempFolder, Photo.PhotosFolderName));
                People.RecreateDirectory(Path.Combine(tempFolder, Story.StoriesFolderName));
                People.RecreateDirectory(Path.Combine(tempFolder, Attachment.AttachmentsFolderName));

                try
                {
                    GedcomImport ged = new GedcomImport();

                    PeopleCollection family2 = new PeopleCollection();
                    SourceCollection source2 = new SourceCollection();
                    RepositoryCollection repository2 = new RepositoryCollection();
                    GedcomHeader header;

                    loaded = ged.Import(out header, family2, source2, repository2, dialog.FileName, appSettings.EnableUTF8);

                    family.CurrentChanged -= People_CurrentChanged;

                    familyCollection.ImportedHeader = header;
                    familyCollection.ExportHeader = People.MakeDefaultHeader("Family Lines"); // TODO take from settings; more details
                        familyCollection.ExportHeader.ContentDescription = header.ContentDescription == null ? new GedcomNoteRecord() : header.ContentDescription.Copy();

                    // KBR Fixes for missing sources/repositories
                    familyCollection.RepositoryCollection = repository2;
                    familyCollection.SourceCollection = source2;
                    familyCollection.PeopleCollection = family2;

                    family = family2;
                    source = source2;
                    repository = repository2;

                    App.Family = family;
                    App.Sources = source2;
                    App.Repositories = repository2;

                    family.CurrentChanged += People_CurrentChanged;

                    familyCollection.FullyQualifiedFilename = string.Empty;  //file name must be familyx, this ensures user is prompted to save file to familyx
                    family.IsDirty = false;
                }
                catch
                {
                    // Could not import the GEDCOM for some reason. Handle
                    // all exceptions the same, display message and continue
                    // without importing the GEDCOM file.
                    MessageBox.Show(this, Properties.Resources.GedcomFailedMessage, Properties.Resources.GedcomFailed, MessageBoxButton.OK, MessageBoxImage.Error);
                    loaded = false;
                }
            }

            CollapseDetailsPanels();
            ShowDetailsPane();
            family.OnContentChanged();
            TheFamilyView.Init(); // TODO this is brute force (Family disconnect?)

            // The collection requires a primary-person, use the first
            // person added to the collection as the primary-person.
            if (family.Count > 0)
                family.Current = family[0];

            TaskBar.Current.Restore();
            UpdateStatus();
            App.canExecuteJumpList = true;

            if (!loaded || family.Count == 0)
            {
                ShowWelcomeScreen();
                UpdateStatus();
            }
        }

        /// <summary>
        /// Load the selected familyx file.
        /// Returns true on sucessful load.
        /// </summary>
        private bool LoadFamily(string fileName)
        {
            giveControlFocus();
            TaskBar.Current.Loading();
            ReleasePhotos();

            //            bool fileLoaded = familyCollection.LoadOPC();

            People newFamily = new People();
            newFamily.FullyQualifiedFilename = fileName;
            bool fileLoaded = newFamily.LoadOPC();

            if (fileLoaded)
            {
                family.CurrentChanged -= People_CurrentChanged;

                // TODO: copy to App.Resources? App.Sources?

                familyCollection.PeopleCollection = newFamily.PeopleCollection;
                familyCollection.ImportedHeader = newFamily.ImportedHeader.Copy();
                familyCollection.ExportHeader = newFamily.ExportHeader.Copy();
                family = familyCollection.PeopleCollection; 
                App.Family = family;
                if (family.Count > 0)
                {
                    family.Current = null;
                    family.Current = family[0]; // magic to force the details pane to focus properly
                }
                family.CurrentChanged += People_CurrentChanged;

                family.Current = family[0];

                familyCollection.FullyQualifiedFilename = fileName;

                TheFamilyView.Init(); // TODO this is brute force (Family disconnect?)
            }
            else
            {
                familyCollection.FullyQualifiedFilename = "";
            }
            UpdateStatus();

            removeControlFocus();
            TaskBar.Current.Restore();

            return fileLoaded;
        }

        /// <summary>
        /// Load the selected family file.
        /// Returns true on sucessful load.
        /// </summary>
        private bool LoadVersion2(string fileName)
        {
            giveControlFocus();
            TaskBar.Current.Loading();
            ReleasePhotos();

            MessageBox.Show(Properties.Resources.OldVersionMessage, Properties.Resources.Compatability, MessageBoxButton.OK, MessageBoxImage.Information);

            familyCollection.FullyQualifiedFilename = fileName;
            bool fileLoaded = familyCollection.LoadVersion2();

            if (fileLoaded)
            {
                familyCollection.FullyQualifiedFilename = Path.ChangeExtension(fileName, Properties.Resources.DefaultFamilyxExtension);
                SaveFamilyAs();
            }
            else
                familyCollection.FullyQualifiedFilename = string.Empty;


            UpdateStatus();

            removeControlFocus();
            TaskBar.Current.Restore();

            return fileLoaded;
        }

        /// <summary>
        /// Merge the selected familyx file and return a summary on success.
        /// </summary>
        private string[,] MergeFamily(string fileName)
        {
            string[,] summary = familyCollection.MergeOPC(fileName);
            return summary;
        }

        #endregion

        #region diagram commands

        /// <summary>
        /// Command handler for FullScreen On
        /// </summary>
        private void FullScreen_Checked(object sender, EventArgs e)
        {
            DetailsPane.Visibility = Visibility.Collapsed;

            // Remove the cloned columns from layers 0
            if (DiagramPane.ColumnDefinitions.Contains(column1CloneForLayer0))
                DiagramPane.ColumnDefinitions.Remove(column1CloneForLayer0);
        }

        /// <summary>
        /// Command handler for FullScreen Off
        /// </summary>
        private void FullScreen_Unchecked(object sender, EventArgs e)
        {
            if (WelcomeUserControl.Visibility != Visibility.Visible)
            {
                if (!DiagramPane.ColumnDefinitions.Contains(column1CloneForLayer0))
                    DiagramPane.ColumnDefinitions.Add(column1CloneForLayer0);

                if (family.Current != null)
                    DetailsControl.DataContext = family.Current;


                DetailsPane.Visibility = Visibility.Visible;
            }

            DetailsControl.SetDefaultFocus();
        }

        /// <summary>
        /// Command handler for hide controls checked.
        /// </summary>
        private void HideControls_Checked(object sender, EventArgs e)
        {
            if (DiagramBorder.Visibility == Visibility.Visible)
            {
                this.DiagramControl.TimeSliderPanel.Visibility = Visibility.Hidden;
                this.DiagramControl.ZoomSliderPanel.Visibility = Visibility.Hidden;
                hideDiagramControls = true;
            }
        }

        /// <summary>
        /// Command handler for hide controls unchecked.
        /// </summary>
        private void HideControls_Unchecked(object sender, EventArgs e)
        {
            if (DiagramBorder.Visibility == Visibility.Visible)
            {
                this.DiagramControl.TimeSliderPanel.Visibility = Visibility.Visible;
                this.DiagramControl.ZoomSliderPanel.Visibility = Visibility.Visible;
                hideDiagramControls = false;
            }
        }

        #endregion

        #region command line handlers

        /// <summary>
        /// Handles the command line arguments.
        /// This allows *.familyx files to be opened via double click if 
        /// Family.Show is registered as the file handler for *.familyx extensions.
        /// The method also handles the Windows 7 JumpList "Tasks".
        /// </summary>
        public void ProcessCommandLines()
        {
            if (App.canExecuteJumpList)
            {

                if (App.args != "/x")
                {
                    if ((App.args.EndsWith(Properties.Resources.DefaultFamilyxExtension) || App.args.EndsWith(Properties.Resources.DefaultFamilyExtension)) && File.Exists(App.args))
                    {

                        bool loaded = true;

                        if (App.args.EndsWith(Properties.Resources.DefaultFamilyxExtension))
                            loaded = LoadFamily(App.args);
                        else if (App.args.EndsWith(Properties.Resources.DefaultFamilyExtension))
                            loaded = LoadVersion2(App.args);

                        if (!loaded)
                        {
                            ShowWelcomeScreen();
                            UpdateStatus();
                        }
                        else
                        {
                            CollapseDetailsPanels();
                            ShowDetailsPane();
                            family.OnContentChanged();
                        }

                        // Do not add non default files to recent files list
                        if (familyCollection.FullyQualifiedFilename.EndsWith(Properties.Resources.DefaultFamilyxExtension))
                        {
                            // Remove the file from its current position and add it back to the top/most recent position.
                            App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                            App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                            BuildOpenMenu();
                            family.IsDirty = false;
                        }

                        if (family.Count == 0)
                            ShowWelcomeScreen();

                        UpdateStatus();
                    }

                    else
                    {
                        switch (App.args)
                        {
                            case "/n":
                                NewCommandLine();
                                break;
                            case "/i":
                                ImportCommandLine();
                                break;
                            case "/o":
                                HideWelcomeScreen();
                                HideNewUserControl();
                                OpenCommandLine();
                                break;
                            default:
                                ShowWelcomeScreen();
                                break;
                        }
                    }
                }
                else
                    ShowWelcomeScreen();
            }
            
        }       

        /// <summary>
        /// Handles loading a file from command line.
        /// </summary>
        public void LoadCommandLine(string fileName)
        {
            LoadFamily(fileName);
        }

        /// <summary>
        /// Handles opening a familyx file from command line.
        /// </summary>
        public void OpenCommandLine()
        {
            OpenFamily();
        }

        /// <summary>
        /// Handles starting a new family from command line.
        /// </summary>
        public void NewCommandLine()
        {
            NewFamily();
        }

        /// <summary>
        /// Handles importing a GEDCOM file from command line.
        /// </summary>
        public void ImportCommandLine()
        {
            GedcomLocalizationImport();
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Displays the Details Pane.
        /// </summary>
        private void ShowDetailsPane()
        {
            // Add the cloned column to layer 0:
            if (!DiagramPane.ColumnDefinitions.Contains(column1CloneForLayer0))
                DiagramPane.ColumnDefinitions.Add(column1CloneForLayer0);

            if (family.Current != null)
                DetailsControl.DataContext = family.Current;

            DetailsPane.Visibility = Visibility.Visible;
            DetailsControl.SetDefaultFocus();

            PersonInfoControl.Visibility = Visibility.Collapsed;

            HideNewUserControl();
            HideWelcomeScreen();

            FullScreen.IsChecked = false;
            
            enableMenus();

        }

        /// <summary>
        /// Hides any visible Details Panels.
        /// </summary>
        private void CollapseDetailsPanels()
        {
            if (DetailsControl.DetailsEdit.Visibility == Visibility.Visible)
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEdit"]).Begin(DetailsControl);
            if (DetailsControl.DetailsEditMore.Visibility == Visibility.Visible)
            {
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEditMore"]).Begin(DetailsControl);
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEdit"]).Begin(DetailsControl);
            }

            if(DetailsControl.DetailsEditRelationship.Visibility==Visibility.Visible)
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEditRelationship"]).Begin(DetailsControl);

            if(DetailsControl.DetailsEditAttachments.Visibility==Visibility.Visible)
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEditAttachments"]).Begin(DetailsControl);

            if(DetailsControl.DetailsEditCitations.Visibility==Visibility.Visible)
                ((Storyboard)DetailsControl.Resources["CollapseDetailsEditCitations"]).Begin(DetailsControl);

            if (this.PersonInfoControl.Visibility == Visibility.Visible)
                ((Storyboard)this.Resources["HidePersonInfo"]).Begin(this);
            if (this.FamilyDataControl.Visibility == Visibility.Visible)
                ((Storyboard)this.Resources["HideFamilyData"]).Begin(this);

        }

        /// <summary>
        /// Disables all buttons on the Details Controls.
        /// </summary>
        private void disableButtons()
        {
            DetailsControl.EditButton.IsEnabled =
                DetailsControl.InfoButton.IsEnabled =
                DetailsControl.FamilyMemberAddButton.IsEnabled =
                DetailsControl.FamilyDataButton.IsEnabled =
                DetailsControl.EditAttachmentsButton.IsEnabled =
                DetailsControl.EditRelationshipsButton.IsEnabled =
                DetailsControl.EditCitationsButton.IsEnabled =
                DetailsControl.EditMoreButton.IsEnabled =
                    false;
        }

        /// <summary>
        /// Enables all buttons on the Details Controls.
        /// </summary>
        private void enableButtons()
        {
            DetailsControl.InfoButton.IsEnabled =
                DetailsControl.FamilyDataButton.IsEnabled =
                DetailsControl.EditAttachmentsButton.IsEnabled =
                DetailsControl.EditRelationshipsButton.IsEnabled =
                DetailsControl.EditCitationsButton.IsEnabled =
                DetailsControl.EditButton.IsEnabled =
                DetailsControl.EditMoreButton.IsEnabled =
                DetailsControl.FamilyMemberAddButton.IsEnabled =
                    true;
        }

        /// <summary>
        /// Enables all menus.
        /// </summary>
        private void enableMenus()
        {
            App.canExecuteJumpList =
                NewMenu.IsEnabled =
                OpenMenu.IsEnabled =
                SaveMenu.IsEnabled =
                PrintMenu.IsEnabled =
                MediaMenu.IsEnabled =
                ThemesMenu.IsEnabled =
                HelpMenu.IsEnabled =
                ViewMenu.IsEnabled =
                    true;
        }

        /// <summary>
        /// Disables all menus.
        /// </summary>
        private void disableMenus()
        {
            App.canExecuteJumpList =
                NewMenu.IsEnabled =
                OpenMenu.IsEnabled =
                SaveMenu.IsEnabled =
                MediaMenu.IsEnabled =
                PrintMenu.IsEnabled =
                ThemesMenu.IsEnabled =
                HelpMenu.IsEnabled =
                ViewMenu.IsEnabled =
                    false;
        }

        /// <summary>
        /// Give a control focus.
        /// </summary>
        private void giveControlFocus()
        {
            disableMenus();
            disableButtons();
            HideFamilyDataControl();
            HidePersonInfoControl();
            HideDetailsPane();
            HideWelcomeScreen();

            PhotoViewerControl.Visibility = Visibility.Hidden;
            StoryViewerControl.Visibility = Visibility.Hidden;
            AttachmentViewerControl.Visibility = Visibility.Hidden;
            DiagramControl.Visibility = Visibility.Hidden;
            TheFamilyView.Visibility = Visibility.Hidden;                    
        }

        /// <summary>
        /// Remove focus from a control.
        /// </summary>
        private void removeControlFocus()
        {
            enableMenus();
            enableButtons();
            ShowDetailsPane();

            DiagramControl.Visibility = UsingFamilyView ? Visibility.Hidden : Visibility.Visible;
            TheFamilyView.Visibility = UsingFamilyView ? Visibility.Visible : Visibility.Hidden;

            if (family.Current != null)
                DetailsControl.DataContext = family.Current;
        }

        /// <summary>
        /// Displays the current file name in the window Title.
        /// </summary>
        private void UpdateStatus()
        {
            // The current file name
            string filename = Path.GetFileName(familyCollection.FullyQualifiedFilename);

            // Default value for Title
            Title = Properties.Resources.FamilyShow;

            // If the Welcome Control is visible, set window Title.
            if (WelcomeUserControl.Visibility == Visibility.Visible)
            {
                family.IsDirty = false;
                Title = Properties.Resources.ApplicationName;
            }
            // In every other case, display the file name as the window Title and "Unsaved" if the file is not saved.
            else
            {
                if (string.IsNullOrEmpty(filename))
                    Title = Properties.Resources.ApplicationName + " " + Properties.Resources.UnsavedStatus;
                else
                    Title = filename + " - " + Properties.Resources.FamilyShow;
            }
        }

        /// <summary>
        /// Hides the Details Pane.
        /// </summary>
        private void HideDetailsPane()
        {
            DetailsPane.Visibility = Visibility.Collapsed;

            //After collapse DetailsPanel, Update DiagramControl's Layout.
            //Without this sentence, DiagramControl will not update its size when it is back to visibile and
            //Grid size inside ScrollViewer could not get updated. Issue 1590
            DiagramControl.UpdateLayout();

            // Remove the cloned columns from layers 0
            if (DiagramPane.ColumnDefinitions.Contains(column1CloneForLayer0))
                DiagramPane.ColumnDefinitions.Remove(column1CloneForLayer0);

            disableMenus();
        }

        /// <summary>
        /// Hides the Family Data Control.
        /// </summary>
        private void HideFamilyDataControl()
        {
            // Uses an animation to hide the Family Data Control
            if (FamilyDataControl.IsVisible)
                ((Storyboard)this.Resources["HideFamilyData"]).Begin(this);
        }

        /// <summary>
        /// Hides the Person Info Control.
        /// </summary>
        private void HidePersonInfoControl()
        {
            // Uses an animation to hide the Family Data Control
            if (PersonInfoControl.IsVisible)
                ((Storyboard)this.Resources["HidePersonInfo"]).Begin(this);
        }

        /// <summary>
        /// Hides the New User Control.
        /// </summary>
        private void HideNewUserControl()
        {
            NewUserControl.Visibility = Visibility.Hidden;

            DiagramControl.Visibility = UsingFamilyView ? Visibility.Hidden : Visibility.Visible;
            TheFamilyView.Visibility = UsingFamilyView ? Visibility.Visible : Visibility.Hidden;

            enableButtons();

            if (family.Current != null)
                DetailsControl.DataContext = family.Current;
        }

        /// <summary>
        /// Shows the New User Control.
        /// </summary>
        private void ShowNewUserControl()
        {
            HideFamilyDataControl();
            HideDetailsPane();
            DiagramControl.Visibility = Visibility.Collapsed;
            TheFamilyView.Visibility = Visibility.Collapsed;
            WelcomeUserControl.Visibility = Visibility.Collapsed;

            if (PersonInfoControl.Visibility == Visibility.Visible)
                ((Storyboard)Resources["HidePersonInfo"]).Begin(this);

            NewUserControl.Visibility = Visibility.Visible;
            NewUserControl.ClearInputFields();
            NewUserControl.SetDefaultFocus();

            // Delete to clear existing files and re-create the necessary folders.
            string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), App.ApplicationFolderName);
            tempFolder = Path.Combine(tempFolder, FamilyLinesLib.App.AppDataFolderName);

            People.RecreateDirectory(tempFolder);
            People.RecreateDirectory(Path.Combine(tempFolder, Photo.PhotosFolderName));
            People.RecreateDirectory(Path.Combine(tempFolder, Story.StoriesFolderName));
            People.RecreateDirectory(Path.Combine(tempFolder, Attachment.AttachmentsFolderName));
        }

        /// <summary>
        /// Shows the Welcome Screen user control and hides the other controls.
        /// </summary>
        private void ShowWelcomeScreen()
        {
            HideDetailsPane();
            HideNewUserControl();
            DiagramControl.Visibility = Visibility.Hidden;
            TheFamilyView.Visibility = Visibility.Hidden;
            WelcomeUserControl.Visibility = Visibility.Visible;
            App.canExecuteJumpList = true;
        }

        /// <summary>
        /// Hides the Welcome Screen.
        /// </summary>
        private void HideWelcomeScreen()
        {
            WelcomeUserControl.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// Builds the Recent Files Menu.
        /// </summary>
        private void BuildOpenMenu()
        {
            // Store the menu items with icons
            MenuItem open = new MenuItem();
            MenuItem import = new MenuItem();
            MenuItem merge = new MenuItem();

            foreach (object element in OpenMenu.Items)
            {
                var item = element as MenuItem;
                if (item != null)
                {
                    if (item.Header.ToString() == Properties.Resources.OpenMenu)
                        open = item;
                    if (item.Header.ToString() == Properties.Resources.MergeMenu)
                        merge = item;
                    if (item.Header.ToString() == Properties.Resources.GedcomMenu)
                        import = item;
                }                
            }

            // Clear existing menu items
            OpenMenu.Items.Clear();

            // Restore menu items with icons
            OpenMenu.Items.Add(open);
            OpenMenu.Items.Add(import);
            OpenMenu.Items.Add(merge);
            
            // Add the recent files to the menu as menu items
            if (App.RecentFiles.Count > 0)
            {
                OpenMenu.Items.Add(new Separator());

                int i = 1;
                foreach (string file in App.RecentFiles)
                {
                    MenuItem item = new MenuItem();
                    item.Header = i + ". " + Path.GetFileName(file);
                    item.CommandParameter = file;
                    item.Click += OpenRecentFile;
                    OpenMenu.Items.Add(item);
                    i++;
                }

                OpenMenu.Items.Add(new Separator());

                MenuItem openMenuItem4 = new MenuItem();
                openMenuItem4.Header = Properties.Resources.ClearRecentFilesMenu;
                openMenuItem4.Click += ClearRecentFiles;
                OpenMenu.Items.Add(openMenuItem4);
            }
        }

        /// <summary>
        /// Builds the Themes Menu.
        /// </summary>
        [Localizable(false)]
        private void BuildThemesMenu()
        {
            MenuItem theme1 = new MenuItem();
            MenuItem theme2 = new MenuItem();

            theme1.Header = Properties.Resources.Black;
            theme1.CommandParameter = @"Themes\Black\BlackResources.xaml";
            theme1.Click += ChangeTheme;

            theme2.Header = Properties.Resources.Silver;
            theme2.CommandParameter = @"Themes\Silver\SilverResources.xaml";
            theme2.Click += ChangeTheme;

            MenuItem theme3 = new MenuItem();
            theme3.Header = Properties.Resources.Metro;
            theme3.CommandParameter = @"Themes\Metro\MetroResources.xaml";
            theme3.Click += ChangeTheme;

            MenuItem theme4 = new MenuItem();
            theme4.Header = "Blue";
            theme4.CommandParameter = @"Themes\Blue\BlueResources.xaml";
            theme4.Click += ChangeTheme;

            MenuItem theme5 = new MenuItem();
            theme5.Header = "Green";
            theme5.CommandParameter = @"Themes\Green\GreenResources.xaml";
            theme5.Click += ChangeTheme;

            MenuItem theme6 = new MenuItem();
            theme6.Header = "Yellow";
            theme6.CommandParameter = @"Themes\Yellow\YellowResources.xaml";
            theme6.Click += ChangeTheme;

            ThemesMenu.Items.Add(theme1);
            ThemesMenu.Items.Add(theme2);
            ThemesMenu.Items.Add(theme3);
            ThemesMenu.Items.Add(theme4);
            ThemesMenu.Items.Add(theme5);
            ThemesMenu.Items.Add(theme6);
        }

        /// <summary>
        /// Releases any images the application has loaded.
        /// These must be released before opening a new file.
        /// </summary>
        private void ReleasePhotos()
        {
            PhotoViewerControl.DisplayPhoto.Source = null;
            PersonInfoControl.DisplayPhoto.Source = null;
        }

        /// <summary>
        /// Prompts the user upon closing the application to save the current family if it has been changed.
        /// </summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Make sure the file is saved before the app is closed.  
            // Allows user to cancel close request, save the file or to close without saving.

            if (!family.IsDirty)
                return;

            MessageBoxResult result = MessageBox.Show(Properties.Resources.NotSavedMessage,
                Properties.Resources.Save, MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Prompt to save if the file has not been saved before, otherwise just save to the existing file.
                if (string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
                {
                    CommonDialog dialog = new CommonDialog();
                    dialog.InitialDirectory = People.ApplicationFolderPath;
                    dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
                    dialog.Title = Properties.Resources.SaveAs;
                    dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                    dialog.ShowSave();

                    if (!string.IsNullOrEmpty(dialog.FileName))
                    {
                        familyCollection.Save(dialog.FileName);
                        // Remove the file from its current position and add it back to the top/most recent position.
                        App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                        App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);
                    }
                }
                else
                {
                    familyCollection.Save(false);

                    // Remove the file from its current position and add it back to the top/most recent position.
                    App.RecentFiles.Remove(familyCollection.FullyQualifiedFilename);
                    App.RecentFiles.Insert(0, familyCollection.FullyQualifiedFilename);

                }
                base.OnClosing(e);
            }

            // Continue with close and don't save.
            if (result == MessageBoxResult.No)
            {
                base.OnClosing(e);
            }

            // Cancel the close if user no longer wants to close.
            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Prompts the user to save the current family if it has been changed.
        /// </summary>
        public void PromptToSave()
        {
            if (!family.IsDirty)
                return;

            MessageBoxResult result = MessageBox.Show(Properties.Resources.NotSavedMessage,
                    Properties.Resources.Save, MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // Prompt to save if the file has not been saved before, otherwise just save to the existing file.
                if (string.IsNullOrEmpty(familyCollection.FullyQualifiedFilename))
                {
                    CommonDialog dialog = new CommonDialog();
                    dialog.InitialDirectory = People.ApplicationFolderPath;
                    dialog.Filter.Add(new FilterEntry(Properties.Resources.FamilyxFiles, Properties.Resources.FamilyxExtension));
                    dialog.Title = Properties.Resources.SaveAs;
                    dialog.DefaultExtension = Properties.Resources.DefaultFamilyxExtension;
                    dialog.ShowSave();

                    if (!string.IsNullOrEmpty(dialog.FileName))
                    {
                        familyCollection.Save(dialog.FileName);

                        if (!App.RecentFiles.Contains(familyCollection.FullyQualifiedFilename))
                        {
                            App.RecentFiles.Add(familyCollection.FullyQualifiedFilename);
                        }
                    }
                    else
                    {
                        familyCollection.Save(false);

                        if (!App.RecentFiles.Contains(familyCollection.FullyQualifiedFilename))
                        {
                            App.RecentFiles.Add(familyCollection.FullyQualifiedFilename);
                        }
                    }
                }
            }
        }

        #endregion

        private void BuildShadowsMenu()
        {
            bool showShadows = Properties.Settings.Default.showShadows;
            // TODO Localization support - no embedded strings
            shadowMenu.Header = showShadows ? "Hide Shadows" : "Show Shadows";
        }

        private void ShowShadows_Click(object sender, RoutedEventArgs e)
        {
            bool showShadows = Properties.Settings.Default.showShadows;
            showShadows = !showShadows;
            Properties.Settings.Default.showShadows = showShadows;
            Properties.Settings.Default.Save(); // part of fix for 1380

            // TODO Localization support - no embedded strings
            shadowMenu.Header = showShadows ? "Hide Shadows" : "Show Shadows";

            // TODO this is my brute-force mechanism to rebuild the diagram - make it better
            DiagramControl.Diagram.OnFamilyContentChanged(null, new ContentChangedEventArgs(null));
        }


        private void LocationConcordance_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry("Text Files (.txt)" /*Properties.Resources.TextFiles*/, 
                                              "*.txt" /*Properties.Resources.TextExt*/));
            dialog.Filter.Add(new FilterEntry(Properties.Resources.AllFiles, Properties.Resources.AllExtension));
            dialog.Title = "Location to Surnames Concordance"; //Properties.Resources.ConcordanceTitle;
            dialog.DefaultExtension = ".txt"; //Properties.Resources.TextDefaultExt;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
                return;

            Experimental.LocationConcordance(dialog.FileName, family);
        }

        private void MapToKML_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry("KML Files (.kml)" /*Properties.Resources.KMLFiles*/, 
                                              "*.kml" /*Properties.Resources.KMLExt*/ ));
            dialog.Filter.Add(new FilterEntry(Properties.Resources.AllFiles, Properties.Resources.AllExtension));
            dialog.Title = "KML file for birth, marriage and death locations"; // Properties.Resources.MapToKMLTitle;
            dialog.DefaultExtension = ".kml"; // Properties.Resources.KMLDefaultExt;
            dialog.ShowSave();

            if (string.IsNullOrEmpty(dialog.FileName))
                return;

            Experimental.DumpKML(dialog.FileName, family);
        }

        #region FamilyView to Details commands

        private void Execute_AddChild(object sender, RoutedEventArgs e)
        {
            // TODO This isn't quite 'kosher': the OriginalSource property has been set up with the type of child to add
            var childProps = e.OriginalSource as Tuple<Person, FamilyMemberComboBoxValue>;
            DetailsControl.AddChild(childProps.Item1, childProps.Item2);
        }

        private void Execute_AddParent(object sender, RoutedEventArgs e)
        {
            // TODO This isn't quite 'kosher': the OriginalSource property has been set up with the type of person to add
            var parentProps = e.OriginalSource as Tuple<FamilyMemberComboBoxValue, Person>;
            DetailsControl.AddParent(parentProps.Item1, parentProps.Item2);
        }

        private void Execute_AddSpouse(object sender, RoutedEventArgs args)
        {
            // TODO This isn't quite 'kosher': the OriginalSource property has been set up with the Human to add a spouse to
            var p = args.OriginalSource as Person;
            DetailsControl.AddSpouse(p);
        }

        private void Execute_EditMarriage(object sender, RoutedEventArgs e)
        {
            // TODO This isn't quite 'kosher': the OriginalSource property has been set up with the Human to view as the 'current' spouse
            var p = e.OriginalSource as Person;
            DetailsControl.EditMarriage(p);
        }
        #endregion

        private void HeaderViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.canExecuteJumpList = false;
            giveControlFocus();
            HeaderViewer.ViewHeader(familyCollection.ImportedHeader, ViewContainer, ViewCallback);
        }

        private void HeaderEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.canExecuteJumpList = false;
            giveControlFocus();
            HeaderEditor.EditHeader(familyCollection.ExportHeader, ViewContainer, ViewCallback);
        }

        private void HeaderNotesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.canExecuteJumpList = false;
            giveControlFocus();
            NotesEditor.Edit(familyCollection.ExportHeader, ViewContainer, ViewCallback);
        }

        /// <summary>
        /// Callback method invoked by a "subwindow" when the user exits.
        /// </summary>
        private void ViewCallback(bool wasSaved)
        {
            family.IsDirty = wasSaved;

            removeControlFocus();
            App.canExecuteJumpList = true;
        }

        private void Execute_ViewAllFacts(object sender, RoutedEventArgs e)
        {
            var factProp = e.OriginalSource as Person;
            if (factProp == null)
                return;
            giveControlFocus();
            ViewAllFacts.ShowFacts = true; // Q&D: must call before setting Target
            ViewAllFacts.Target = factProp;
            ViewAllFacts.Visibility = Visibility.Visible;
        }

        private void ViewAllFacts_Finish(object sender, RoutedEventArgs e)
        {
            ViewAllFacts.Visibility = Visibility.Hidden;
            removeControlFocus();
        }

        private void Execute_ViewAllEvents(object sender, RoutedEventArgs e)
        {
            var factProp = e.OriginalSource as Person;
            if (factProp == null)
                return;
            giveControlFocus();
            ViewAllFacts.ShowFacts = false; // Q&D: must call before setting Target
            ViewAllFacts.Target = factProp;
            ViewAllFacts.Visibility = Visibility.Visible;
        }

        private void Execute_EditAllDetails(object sender, RoutedEventArgs e)
        {
            var factProps = e.OriginalSource as Tuple<Person, GEDAttribute>;
            if (factProps == null)
                return;

            giveControlFocus();
            EditFactDetails.Target = factProps.Item1;
            EditFactDetails.Event = factProps.Item2;
            EditFactDetails.Visibility = Visibility.Visible;
        }

        private void EditFactDetails_Finish(object sender, RoutedEventArgs e)
        {
            EditFactDetails.Visibility = Visibility.Hidden;
            removeControlFocus();
        }
    }
}
