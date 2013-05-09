using System;
using System.IO;
using System.Windows;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for Extract.xaml
    /// </summary>
    public partial class Extract
    {
        public Extract()
        {
            InitializeComponent();
        }

        #region routed events

        public static readonly RoutedEvent ExtractButtonClickEvent = EventManager.RegisterRoutedEvent(
            "ExtractButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Extract));

        public static readonly RoutedEvent CancelButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CancelButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Extract));

        // Expose this event for this control's container
        public event RoutedEventHandler CancelButtonClick
        {
            add { AddHandler(CancelButtonClickEvent, value); }
            remove { RemoveHandler(CancelButtonClickEvent, value); }
        }

        #endregion

        #region methods

        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
            ExtractFiles();
            Clear();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
            Clear();
        }

        #endregion

        #region helper methods

        // TODO rename option controls intelligently
        private void Clear()
        {
            Option1.IsChecked = true;
            Option2.IsChecked = true;
            Option3.IsChecked = true;
            Option4.IsChecked = false;
            Option5.IsChecked = true;
        }

        // TODO refactor for DRY - individual vs family completely duplicated
        // TODO need exception handling - failure to create directory, failure to copy file, etc
        private void ExtractFiles()
        {
            //default options
            bool extractPhotos = Option1.IsChecked.Value;
            bool extractStories = Option2.IsChecked.Value;
            bool extractAttachments = Option3.IsChecked.Value;
            bool openFolderAfterExtraction = Option5.IsChecked.Value;
            bool currentPersonOnly = Option4.IsChecked.Value;

            string folderName = Properties.Resources.Unknown + " (" + DateTime.Now.Day + "-" + DateTime.Now.Month  + "-" + DateTime.Now.Year + ")";
            string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\" + App.ApplicationFolderName;

            if (!string.IsNullOrEmpty(App.FamilyCollection.FullyQualifiedFilename))
            {
                folderName = Path.GetFileNameWithoutExtension(App.FamilyCollection.FullyQualifiedFilename);
                folderPath = Path.GetDirectoryName(App.FamilyCollection.FullyQualifiedFilename);
            }

            if (currentPersonOnly && App.Family.Current != null)
                folderName = folderName + " - " + App.Family.Current.Name;

            string contentpath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\" + App.ApplicationFolderName + @"\" + App.AppDataFolderName;


            string folderToExtractTo = Path.Combine(folderPath, folderName);
            if (Directory.Exists(folderToExtractTo))
            {
                int i=1;
                string newPath;

                do
                {
                    newPath = Path.Combine(folderPath, folderName + " (" + i + ")");
                    i++;
                }
                while (Directory.Exists(newPath));
            }

            DirectoryInfo extractedFileLocation = Directory.CreateDirectory(folderToExtractTo);

            string[] photosToExtract = null;
            string[] storiesToExtract = null;
            string[] attachmentsToExtract = null;

            if (!currentPersonOnly)
            {

                if (Directory.Exists(Path.Combine(contentpath, Photo.PhotosFolderName)))
                    photosToExtract = Directory.GetFiles(Path.Combine(contentpath, Photo.PhotosFolderName), "*", SearchOption.AllDirectories);
                if (Directory.Exists(Path.Combine(contentpath, Story.StoriesFolderName)))
                    storiesToExtract = Directory.GetFiles(Path.Combine(contentpath, Story.StoriesFolderName), "*", SearchOption.AllDirectories);
                if (Directory.Exists(Path.Combine(contentpath, Attachment.AttachmentsFolderName)))
                    attachmentsToExtract = Directory.GetFiles(Path.Combine(contentpath, Attachment.AttachmentsFolderName), "*", SearchOption.AllDirectories);

                if (extractAttachments && attachmentsToExtract != null)
                {
                    Directory.CreateDirectory(Path.Combine(folderToExtractTo, Attachment.AttachmentsFolderName));

                    foreach (string file in attachmentsToExtract)
                    {
                        try
                        {
                            FileInfo f = new FileInfo(file);
                            f.CopyTo(Path.Combine(Path.Combine(folderToExtractTo, Attachment.AttachmentsFolderName), Path.GetFileName(file)), true);
                        }
                        catch { }
                    }
                }

                if (extractPhotos && photosToExtract != null)
                {
                    string destPath = Path.Combine(folderToExtractTo, Photo.PhotosFolderName);
                    Directory.CreateDirectory(destPath);

                    foreach (string file in photosToExtract)
                    {
                        try
                        {
                            FileInfo f = new FileInfo(file);
                            string destFile = Path.Combine(destPath, Path.GetFileName(file));
                            f.CopyTo(destFile, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(string.Format("Photo copy failure:{0}|{1}", file, e.Message));
                        }
                    }
                }

                if (extractStories && storiesToExtract != null)
                {
                    string destPath = Path.Combine(folderToExtractTo, Story.StoriesFolderName);
                    Directory.CreateDirectory(destPath); // TODO no exception handling

                    foreach (string file in storiesToExtract)
                    {
                        try
                        {
                            FileInfo f = new FileInfo(file);
                            f.CopyTo(Path.Combine(Path.Combine(folderToExtractTo, Story.StoriesFolderName), Path.GetFileName(file)), true);
                        }
                        catch { }
                    }
                }
            }
            else
            {
                if (App.Family.Current != null) // TODO when would this be true??
                {
                    var photoFold = Path.Combine(folderToExtractTo, Photo.PhotosFolderName);
                    var attachFold = Path.Combine(folderToExtractTo, Attachment.AttachmentsFolderName);
                    var storyFold = Path.Combine(folderToExtractTo, Story.StoriesFolderName);
                    Directory.CreateDirectory(attachFold);
                    Directory.CreateDirectory(photoFold);
                    Directory.CreateDirectory(storyFold);

                    foreach (Photo p in App.Family.Current.Photos)
                    {
                        string file = p.FullyQualifiedPath;

                        try
                        {
                            FileInfo f = new FileInfo(file);
                            var destFileName = Path.Combine(photoFold, Path.GetFileName(file));
                            f.CopyTo(destFileName, true);
                        }
                        catch { }
                    }

                    try
                    {
                        FileInfo f = new FileInfo(App.Family.Current.Story.AbsolutePath);
                        var destFileName = Path.Combine(storyFold, Path.GetFileName(App.Family.Current.Story.AbsolutePath));
                        f.CopyTo(destFileName, true);
                    }
                    catch { }

                    foreach (Attachment a in App.Family.Current.Attachments)
                    {
                        string file = a.FullyQualifiedPath;

                        try
                        {
                            FileInfo f = new FileInfo(file);
                            var destFileName = Path.Combine(attachFold, Path.GetFileName(file));
                            f.CopyTo(destFileName, true);
                        }
                        catch { }
                    }
                }
            }

            if (openFolderAfterExtraction)
            {
                try
                {
                    System.Diagnostics.Process.Start(extractedFileLocation.FullName);
                }
                catch { }
            }
        }

        #endregion
    }
}