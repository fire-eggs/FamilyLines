using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace KBS.FamilyLines
{
    public static class PrintHelper
    {
        private static PageMediaSize A4PaperSize = new PageMediaSize(816, 1248);
        public static void PrintPreview(Window owner, FrameworkElement data)
        {
            using (MemoryStream xpsStream = new MemoryStream())
            {
                using (Package package = Package.Open(xpsStream, FileMode.Create, FileAccess.ReadWrite))
                {
                    string packageUriString = "memorystream://data.xps";
                    Uri packageUri = new Uri(packageUriString);
                    PackageStore.AddPackage(packageUri, package);
                    XpsDocument xpsDocument = new XpsDocument(package, CompressionOption.Maximum, packageUriString);
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(xpsDocument);
                    PrintTicket printTicket = new PrintTicket();
                    printTicket.PageMediaSize = A4PaperSize;

                    writer.Write(data, printTicket);
                    //                    Form visual = new Form(data);
                    //                    writer.Write(visual, printTicket);

                    FixedDocumentSequence document = xpsDocument.GetFixedDocumentSequence();
                    xpsDocument.Close();

                    PrintPreviewWindow printPreviewWnd = new PrintPreviewWindow(document);
                    printPreviewWnd.Owner = owner;
                    printPreviewWnd.ShowDialog();
                    PackageStore.RemovePackage(packageUri);
                }
            }
        }
    }
}
