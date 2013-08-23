/*
 * Family.Show derived code provided under MS-PL license.
 */
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shell;
using System.Xml.Serialization;
using KBS.FamilyLines.Controls;
using KBS.FamilyLinesLib;
using System.Reflection;
using KBS.FamilyLines.Properties;

namespace KBS.FamilyLines
{
    [Localizable(false)]
    internal partial class App : Application
    {
        #region fields
        
        // The name of the application folder.  This folder is used to save the files 
        // for this application such as the photos, stories and family data.
        internal const string ApplicationFolderName = "FamilyLines";
        
        internal const string AppDataFolderName = "Family Data";
        
        internal const string SampleFilesFolderName = "Sample Files";

        // The main list of family members that is shared for the entire application.
        // The FamilyCollection and Family fields are accessed from the same thread,
        // so suppressing the CA2211 code analysis warning.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static People FamilyCollection = new People();
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static PeopleCollection Family = FamilyCollection.PeopleCollection;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static SourceCollection Sources = FamilyCollection.SourceCollection;
        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static RepositoryCollection Repositories = FamilyCollection.RepositoryCollection;

        // The number of recent files to keep track of.
        private const int NumberOfRecentFiles = 5;
  
        // The path to the recent files file.
        private readonly static string RecentFilesFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),Path.Combine(ApplicationFolderName, "RecentFiles.xml"));

        // The global list of recent files.
        private static StringCollection recentFiles = new StringCollection();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static string args;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        public static bool canExecuteJumpList = true;

        #endregion        

        #region Properties

        internal MainWindow Window
        {
            get { return MainWindow as MainWindow; }
        }        

        #endregion

        // A method to handle command line arguments.
        public void ProcessArgs(string[] args, bool firstInstance)
        {
            if (args.Length > 0)
            {
                App.args = Convert.ToString(args[0]);
            }
            else
            {
                App.args = "/x";
            }

            if (Window != null)
            {
                Window.ProcessCommandLines();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            LoadLanguageResources();
            InstallSampleFiles();
            CleanUpTempDirectory();
            CreateWorkingDirectory();

            // Load the collection of recent files.
            LoadRecentFiles();

            InitializeDefaultTheme();           

            // Create and show the application's main window            
            var window = new MainWindow();            
            window.Show();

            // In Windows 7 make use of new TaskBar and JumpList features.
            InitializeTaskBar(window);

            base.OnStartup(e);
        }

        private void InitializeDefaultTheme()
        {
            Settings appSettings = FamilyLines.Properties.Settings.Default;

            if (!string.IsNullOrEmpty(appSettings.Theme))
            {
                try
                {
                    ResourceDictionary rd = new ResourceDictionary();
                    rd.MergedDictionaries.Add(Application.LoadComponent(new Uri(appSettings.Theme, UriKind.Relative)) as ResourceDictionary);
                    Application.Current.Resources = rd;
                }
                catch { }
            }
        }

        private void InitializeTaskBar(Window window)
        {
            string systemFolder = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string applicationFilePath = Assembly.GetExecutingAssembly().Location;

            // KBR 12/31/2012 Use the .NET 4.0 classes instead of the WindowsAPICodePack
            var jtask1 = new JumpTask();
            jtask1.Title = FamilyLines.Properties.Resources.StartANewFamilyTree;
            jtask1.ApplicationPath = applicationFilePath;
            jtask1.Arguments = "/n";
            jtask1.IconResourcePath = Path.Combine(systemFolder, "shell32.dll");
            jtask1.IconResourceIndex = 0;

            var jtask2 = new JumpTask();
            jtask2.Title = FamilyLines.Properties.Resources.OpenMenu;
            jtask2.ApplicationPath = applicationFilePath;
            jtask2.Arguments = "/o";
            jtask2.IconResourcePath = Path.Combine(systemFolder, "shell32.dll");
            jtask2.IconResourceIndex = 4;

            var jtask3 = new JumpTask();
            jtask3.Title = FamilyLines.Properties.Resources.GedcomMenu;
            jtask3.Description = FamilyLines.Properties.Resources.GedcomMenu;
            jtask3.ApplicationPath = applicationFilePath;
            jtask3.Arguments = "/i";
            jtask3.IconResourcePath = Path.Combine(systemFolder, "shell32.dll");
            jtask3.IconResourceIndex = 4;

            var jTasks = new[] {jtask1, jtask2, jtask3};
            TaskBar.Create(window, null, jTasks);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SaveRecentFiles();
            CleanUpTempDirectory();
            base.OnExit(e);
        }

        public void Activate()
        {
            // Reactivate application's main window
            this.MainWindow.Activate();
            this.MainWindow.Focus();
        }

        private void LoadLanguageResources()
        {
            // Load language resources and use en-US incase of errors.
            KBS.FamilyLines.Properties.Resources.Culture = new CultureInfo("en-US");
            KBS.FamilyLinesLib.Properties.Resources.Culture = new CultureInfo("en-US");

            if (File.Exists(KBS.FamilyLines.Properties.Settings.Default.LanguagesFileName))
            {
                try
                {
                    KBS.FamilyLines.Properties.Resources.Culture = new CultureInfo(KBS.FamilyLines.Properties.Settings.Default.Language);
                    KBS.FamilyLinesLib.Properties.Resources.Culture = new CultureInfo(KBS.FamilyLines.Properties.Settings.Default.Language);
                }
                catch
                {
                    // Error with selected language, reset to default.
                    KBS.FamilyLines.Properties.Settings.Default.Language = "en-US";
                    KBS.FamilyLines.Properties.Settings.Default.Save();
                }
            }
            else
            {
                KBS.FamilyLines.Properties.Settings.Default.Language = "en-US";
                KBS.FamilyLines.Properties.Settings.Default.Save();
            }
        }

        #region methods

        /// <summary>
        /// Gets the list of recent files.
        /// </summary>
        public static StringCollection RecentFiles
        {
            get { return recentFiles; }
        }

        /// <summary>
        /// Gets the collection of themes
        /// </summary>
        public static NameValueCollection Themes
        {
            get
            {
                NameValueCollection themes = new NameValueCollection();

                foreach (string folder in Directory.GetDirectories("Themes"))
                {
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        if (string.Compare(fileInfo.Extension, KBS.FamilyLines.Properties.Resources.XamlExtension,
                            true, CultureInfo.InvariantCulture) == 0)
                        {
                            // Use the first part of the resource file name for the menu item name.
                            themes.Add(fileInfo.Name.Remove(fileInfo.Name.IndexOf("Resources")),
                                 Path.Combine(folder, fileInfo.Name));
                        }
                    }
                }

                return themes;
            }
        }

        /// <summary>
        /// Return the animation duration. The duration is extended
        /// if special keys are currently pressed (for demo purposes)  
        /// otherwise the specified duration is returned. 
        /// </summary>
        public static TimeSpan GetAnimationDuration(double milliseconds)
        {
            return TimeSpan.FromMilliseconds(
                Keyboard.IsKeyDown(Key.F12) ?
                milliseconds * 5 : milliseconds);
        }

        /// <summary>
        /// Load the list of recent files from disk.
        /// </summary>
        public static void LoadRecentFiles()
        {
            if (File.Exists(RecentFilesFilePath))
            {
                // Load the Recent Files from disk
                XmlSerializer ser = new XmlSerializer(typeof(StringCollection));
                using (TextReader reader = new StreamReader(RecentFilesFilePath))
                {
                    recentFiles = (StringCollection)ser.Deserialize(reader);
                }

                // Remove files from the Recent Files list that no longer exists.
                for (int i = 0; i < recentFiles.Count; i++)
                {
                    if (!File.Exists(recentFiles[i]))
                        recentFiles.RemoveAt(i);
                }

                // Only keep the 5 most recent files, trim the rest.
                while (recentFiles.Count > NumberOfRecentFiles)
                          recentFiles.RemoveAt(NumberOfRecentFiles);

            }
        }

        /// <summary>
        /// Save the list of recent files to disk.
        /// </summary>
        public static void SaveRecentFiles()
        {
            XmlSerializer ser = new XmlSerializer(typeof(StringCollection));
            using (TextWriter writer = new StreamWriter(RecentFilesFilePath))
            {
                ser.Serialize(writer, recentFiles);
            }
        }

        /// <summary>
        /// Create the working directory the first time the program runs
        /// </summary>
        private static void CreateWorkingDirectory()
        {
            // Full path to the document file location.
            string location = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), ApplicationFolderName);

            // Return right away if the data file already exist.
            if (Directory.Exists(location))
                return;

            try
            {
                // Creates the working directory
                Directory.CreateDirectory(location);
            }
            catch
            {
                // Could not create the working directory
            }
        }

        /// <summary>
        /// Clean up all temp files when program terminates or starts.
        /// </summary>
        private static void CleanUpTempDirectory()
        {
            string appLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    App.ApplicationFolderName);
            appLocation = Path.Combine(appLocation, App.AppDataFolderName);

            try
            {
                // Creates the working directory
                if (Directory.Exists(appLocation))
                    Directory.Delete(appLocation, true);
                Directory.CreateDirectory(appLocation);
            }
            catch
            {
                // Could not create the working directory
            }
        }

        /// <summary>
        /// Install sample files the first time the application runs.
        /// </summary>
        private static void InstallSampleFiles()
        {
            // Full path to the document file location.
            string location = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), ApplicationFolderName);

            location = Path.Combine(location, SampleFilesFolderName);

            // Return right away if the data file already exist.
            if (Directory.Exists(location))
                return;

            try
            {
                // Sample data files.
                Directory.CreateDirectory(location);
                CreateSampleFile(location, "Windsor.familyx", FamilyLines.Properties.Resources.WindsorSampleFile);
                CreateSampleFile(location, "Kennedy.ged", FamilyLines.Properties.Resources.KennedySampleFile);
            }

            catch
            {
                // Could not install the sample files, handle all exceptions the
                // same, ignore and continue without installing the sample files.
            }
        }

        /// <summary>
        /// Extract the sample family files from the executable and write it to the file system.
        /// </summary>
        private static void CreateSampleFile(string location, string fileName, byte[] fileContent)
        {
            // Full path to the sample file.
            string path = Path.Combine(location, fileName);

            // Return right away if the file already exists.
            if (File.Exists(path))
                return;

            // Create the file.
            using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                writer.Write(fileContent);
            }
        }        

        /// <summary>
        /// Converts string to date time object using DateTime.TryParse.  
        /// Also accepts just the year for dates. 1977 = 1/1/1977.
        /// </summary>
        internal static DateTime StringToDate(string dateString)
        {
            //Append first month and day if just the year was entered.
            if (dateString.Length == 4)
                dateString = "1/1/" + dateString;

            DateTime date;
            DateTime.TryParse(dateString, out date);

            return date;
        }

        /// <summary>
        /// Converts a DateTime to a short string.  If DateTime is null, returns an empty string.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal static string DateToString(DateTime? date)
        {
            return date == null ? string.Empty : date.Value.ToShortDateString();
        }

        /// <summary>
        /// Replaces spaces and { } in file names which break relative paths
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static string ReplaceEncodedCharacters(string fileName)
        {
            //fileName = fileName.Replace(" ", "");
            //fileName = fileName.Replace("{", "");
            //fileName = fileName.Replace("}", "");
            return fileName;
        }

        /// <summary>
        /// Determines if an image file is supported based on its extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static bool IsPhotoFileSupported(string fileName)
        {
            string extension = Path.GetExtension(fileName);

            if (string.Compare(extension, ".jpg", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".jpeg", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".png", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".gif", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".tiff", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".tif", true, CultureInfo.InvariantCulture) == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Determines if an attachment file is supported based on its extension.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        internal static bool IsAttachmentFileSupported(string fileName)
        {
            
            string extension = System.IO.Path.GetExtension(fileName);

            // Only allow certain file types
            if (string.Compare(extension, ".docx", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".xlsx", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".pptx", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".odt", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".ods", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".odp", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".doc", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".xls", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".ppt", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".txt", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".htm", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".html", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".pdf", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".xps", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".rtf", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".kml", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".kmz", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".jpg", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".jpeg", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".png", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".gif", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".tiff", true, CultureInfo.InvariantCulture) == 0 ||
                string.Compare(extension, ".tif", true, CultureInfo.InvariantCulture) == 0)
                return true;

            return false;
        }

    }

    #endregion
}