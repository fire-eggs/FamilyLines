using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace KBS.FamilyLinesLib
{
    /// <summary>
    /// Simple representation of a serializable photo associated with the Person class
    /// </summary>
    [Serializable]
    public class Photo : INotifyPropertyChanged
    {
        #region Fields and Constants

        public const string PhotosFolderName = "Images";
        private string relativePath;
        private bool isAvatar;
        
        #endregion

        #region Properties

        /// <summary>
        /// The relative path to the photo.
        /// </summary>
        public string RelativePath
        {
            get { return relativePath; }
            set
            {
                if (relativePath != value)
                {
                    relativePath = value;
                    OnPropertyChanged("relativePath");
                }
            }
        }

        // KBR 03/18/2012 Apply the essence of patch 1784 by PandaWood.
		public static string FamilyFilePath;
		public static void SetFamilyFileNamePath(string familyFilePath)
		{
			FamilyFilePath = familyFilePath;
		}

        /// <summary>
        /// The fully qualified path to the photo.
        /// </summary>
        [XmlIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "value")]
        public string FullyQualifiedPath
        {
            get
            {

                string tempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                App.ApplicationFolderName);
                tempFolder = Path.Combine(tempFolder, App.AppDataFolderName);

                var path1 = Path.Combine(tempFolder, relativePath);

                // KBR 03/18/2012 Apply the essence of patch 1784 by PandaWood. If the fully qualified path doesn't exist,
                // use the relativepath. This allows transporting a "Family.Show" package, with photos, to other people.
                // I believe this works only with "Version2" format, not the "V3" format???
                if (File.Exists(path1))
                    return path1;
			    return Path.Combine(FamilyFilePath, relativePath);
            }
            set
            {
                // This empty setter is needed for serialization.
            }
        }

        /// <summary>
        /// Whether the photo is the avatar photo or not.
        /// </summary>
        public bool IsAvatar
        {
            get { return isAvatar; }
            set
            {
                if (isAvatar != value)
                {
                    isAvatar = value;
                    OnPropertyChanged("IsAvatar");
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Empty constructor is needed for serialization
        /// </summary>
        public Photo() { }

        /// <summary>
        /// Constructor for Photo. Copies the photoPath to the images folder
        /// </summary>
        public Photo(string photoPath)
        {

            if (!string.IsNullOrEmpty(photoPath))
                // Copy the photo to the images folder
                this.relativePath = Copy(photoPath);
        }

        #endregion

        #region Methods
        
        public override string ToString()
        {
            return FullyQualifiedPath;
        }
        //copy from photo location to foldername
        public static string Copy(string fileName, string folderName)
       {
           string photoRelLocation="";
            

            // Absolute path to the application folder
            string appLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                App.ApplicationFolderName);
            appLocation = Path.Combine(appLocation, App.AppDataFolderName);

            // Absolute path to the photos folder
            string photoLocation = Path.Combine(appLocation, PhotosFolderName);

            // Fully qualified path to the new photo file
            string photoFullPath = Path.Combine(photoLocation, fileName);

            // The photo file being copied
            FileInfo fi = new FileInfo(photoFullPath);

            // Relative path to the new photo file

            //string photoRelLocation = Path.Combine(folderName, App.ReplaceEncodedCharacters(fi.Name));

        //    // Create the appLocation directory if it doesn't exist
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            
        //    // Copy the photo.
           try
            {
                string photoName = Path.GetFileName(photoFullPath);
                string photoNameNoExt = Path.GetFileNameWithoutExtension(photoFullPath);
                string photoNameExt = Path.GetExtension(photoFullPath);

                int i = 1;
                string destphotoFullPath = Path.Combine(folderName, photoName);
                photoRelLocation = Path.Combine(folderName, photoName);
                if (File.Exists(destphotoFullPath))
                {
                    do
                    {
                        photoName = photoNameNoExt + "(" + i + ")" + photoNameExt;  //don't overwrite existing files, append (#) to file if exists.
                        photoRelLocation = Path.Combine(folderName, photoName);
                        destphotoFullPath = Path.Combine(folderName, photoName);
                        i++;
                    }
                    while (File.Exists(destphotoFullPath));

                }
                fi.CopyTo(destphotoFullPath, true);

            }
            catch
            {
        //        // Could not copy the photo. Handle all exceptions 
        //        // the same, ignore and continue.
            }

            return photoRelLocation;
        }
        /// <summary>
        /// Copies the photo file to the application photos folder. 
        /// Returns the relative path to the copied photo.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private static string Copy(string fileName)
        {
		
            // The photo file being copied
            FileInfo fi = new FileInfo(fileName);

            // Absolute path to the application folder
            string appLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                App.ApplicationFolderName);
            appLocation = Path.Combine(appLocation, App.AppDataFolderName);

            // Absolute path to the photos folder
            string photoLocation = Path.Combine(appLocation, PhotosFolderName);

            // Fully qualified path to the new photo file
            string photoFullPath = Path.Combine(photoLocation, App.ReplaceEncodedCharacters(fi.Name));

            // Relative path to the new photo file
		
			string photoRelLocation = Path.Combine(PhotosFolderName, App.ReplaceEncodedCharacters(fi.Name));

            // Create the appLocation directory if it doesn't exist
            if (!Directory.Exists(appLocation))
                Directory.CreateDirectory(appLocation);

            // Create the photos directory if it doesn't exist
            if (!Directory.Exists(photoLocation))
                Directory.CreateDirectory(photoLocation);
            
            // Copy the photo.
            try
            {
                string photoName = Path.GetFileName(photoFullPath);
                string photoNameNoExt = Path.GetFileNameWithoutExtension(photoFullPath);
                string photoNameExt = Path.GetExtension(photoFullPath);

                int i = 1;

                if (File.Exists(photoFullPath))
                {
                    do
                    {
                        photoName = photoNameNoExt + "(" + i + ")" + photoNameExt;  //don't overwrite existing files, append (#) to file if exists.
                        photoRelLocation = Path.Combine(PhotosFolderName, photoName);
                        photoFullPath = Path.Combine(photoLocation, photoName);
                        i++;
                    }
                    while (File.Exists(photoFullPath));
                    
                }
                fi.CopyTo(photoFullPath, true);

            }
            catch
            {
                // Could not copy the photo. Handle all exceptions 
                // the same, ignore and continue.
            }

            return photoRelLocation;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    /// <summary>
    /// Collection for photos.
    /// </summary>
    [Serializable]
    public class PhotoCollection : ObservableCollection<Photo>
    {
    }
}
