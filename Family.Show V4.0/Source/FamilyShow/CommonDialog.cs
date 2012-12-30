/*
 * Displays the common Open and SaveAs dialogs using the Vista-style dialogs.
 * This is accomplished by pinvoking GetOpenFileName and GetSaveFileName and 
 * not specifying a hook. The .NET Microsoft.Win32 OpenFileDialog and 
 * SaveFileDialog classes display the old-style instead of the new Vista-style 
 * dialogs.
 * 
 * If a hook is required, see the Vista Bridge sample at 
 * http://msdn2.microsoft.com/en-us/library/ms756482.aspx that contains 
 * managed wrappers for a number of new Windows Vista APIs, including 
 * IFileDialog, IFileOpenDialog and IFileSaveDialog.
*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Microsoft.FamilyShow
{
    /// <summary>
    /// One item in the common dialog filter.
    /// </summary>
    public class FilterEntry
    {
        private string display;
        private string extension;

        public string Display
        {
            get { return display; }
        }
        
        public string Extension
        {
            get { return extension; }
        }

        public FilterEntry(string display, string extension)
        {
            this.display = display;
            this.extension = extension;
        }
    }

    /// <summary>
    /// Displays the common Open and SaveAs dialogs using the Vista-style dialogs.
    /// </summary>
    class CommonDialog
    {
        #region fields

        // Structure used when displaying Open and SaveAs dialogs.
        private OpenFileName ofn = new OpenFileName();

        // List of filters to display in the dialog.
        private List<FilterEntry> filter = new List<FilterEntry>();
        
        #endregion

        #region properties

        public List<FilterEntry> Filter
        {
            get { return filter; }
        }

        public string Title
        {
            set { ofn.title = value; }
        }

        public string InitialDirectory
        {
            set { ofn.initialDir = value; }
        }

        public string DefaultExtension
        {
            set { ofn.defExt = value; }
        }

        public string FileName
        {
            get { return ofn.file; }
        }

        #endregion
    
        #region pinvoke details

        private enum OpenFileNameFlags
        {
            OFN_READONLY = 0x00000001,
            OFN_OVERWRITEPROMPT = 0x00000002,
            OFN_HIDEREADONLY = 0x00000004,
            OFN_NOCHANGEDIR = 0x00000008,
            OFN_SHOWHELP = 0x00000010,
            OFN_ENABLEHOOK = 0x00000020,
            OFN_ENABLETEMPLATE = 0x00000040,
            OFN_ENABLETEMPLATEHANDLE = 0x00000080,
            OFN_NOVALIDATE = 0x00000100,
            OFN_ALLOWMULTISELECT = 0x00000200,
            OFN_EXTENSIONDIFFERENT = 0x00000400,
            OFN_PATHMUSTEXIST = 0x00000800,
            OFN_FILEMUSTEXIST = 0x00001000,
            OFN_CREATEPROMPT = 0x00002000,
            OFN_SHAREAWARE = 0x00004000,
            OFN_NOREADONLYRETURN = 0x00008000,
            OFN_NOTESTFILECREATE = 0x00010000,
            OFN_NONETWORKBUTTON = 0x00020000,
            OFN_NOLONGNAMES = 0x00040000,
            OFN_EXPLORER = 0x00080000,
            OFN_NODEREFERENCELINKS = 0x00100000,
            OFN_LONGNAMES = 0x00200000,
            OFN_ENABLEINCLUDENOTIFY = 0x00400000,
            OFN_ENABLESIZING = 0x00800000,
            OFN_DONTADDTORECENT = 0x02000000,
            OFN_FORCESHOWHIDDEN = 0x10000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            internal int structSize;
            internal IntPtr owner;
            internal IntPtr instance;
            internal string filter;
            internal string customFilter;
            internal int maxCustFilter;
            internal int filterIndex;
            internal string file;
            internal int maxFile;
            internal string fileTitle;
            internal int maxFileTitle;
            internal string initialDir;
            internal string title;
            internal int flags;
            internal Int16 fileOffset;
            internal Int16 fileExtension;
            internal string defExt;
            internal IntPtr custData;
            internal IntPtr hook;
            internal string templateName;
        }

        private static class NativeMethods
        {
            [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

            [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);
        }
        
        #endregion

        public CommonDialog()
        {
            // Initialize structure that is passed to the API functions.
            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.file = new String(new char[260]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new String(new char[100]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
        }

        /// <summary>
        /// Display the Vista-style common Open dialog.
        /// </summary>
        public bool ShowOpen()
        {
            SetFilter();
            ofn.flags = (int)OpenFileNameFlags.OFN_FILEMUSTEXIST;
            if (Application.Current.MainWindow != null)
                ofn.owner = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            return NativeMethods.GetOpenFileName(ofn);
        }

        /// <summary>
        /// Display the Vista-style common Save As dialog.
        /// </summary>
        public bool ShowSave()
        {
            SetFilter();
            ofn.flags = (int)(OpenFileNameFlags.OFN_PATHMUSTEXIST | OpenFileNameFlags.OFN_OVERWRITEPROMPT);
            if (Application.Current.MainWindow != null)
                ofn.owner = new WindowInteropHelper(Application.Current.MainWindow).Handle;
            return NativeMethods.GetSaveFileName(ofn);
        }

        /// <summary>
        /// Set the low level filter with the filter collection.
        /// </summary>
        private void SetFilter()
        {
            StringBuilder sb = new StringBuilder();
            foreach (FilterEntry entry in this.filter)
                sb.AppendFormat("{0}\0{1}\0", entry.Display, entry.Extension);
            sb.Append("\0\0");
            ofn.filter = sb.ToString();
        }


    }
}
