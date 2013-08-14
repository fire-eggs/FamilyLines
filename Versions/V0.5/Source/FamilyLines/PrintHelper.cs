using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using System.Windows.Xps.Serialization;
using Binding = System.Windows.Data.Binding;
using FlowDirection = System.Windows.FlowDirection;

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

        private static string _previewWindowXaml =
            @"<Window
        xmlns                 ='http://schemas.microsoft.com/netfx/2007/xaml/presentation'
        xmlns:x               ='http://schemas.microsoft.com/winfx/2006/xaml'
        Title                 ='Print Preview - @@TITLE'
        Height                ='200'
        Width                 ='300'
        WindowStartupLocation ='CenterOwner'>
        <DocumentViewer Name='dv1'/>
     </Window>";

        public static void DoPreview(string title, FrameworkElement visual)
        {
            string fileName = System.IO.Path.GetRandomFileName();
//            FlowDocumentScrollViewer visual = (FlowDocumentScrollViewer)(_parent.FindName("fdsv1"));
            try
            {
                // write the XPS document
                using (XpsDocument doc = new XpsDocument(fileName, FileAccess.ReadWrite))
                {
                    XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
                    writer.Write(visual);
                }

                // Read the XPS document into a dynamically generated
                // preview Window 
                using (XpsDocument doc = new XpsDocument(fileName, FileAccess.Read))
                {
                    FixedDocumentSequence fds = doc.GetFixedDocumentSequence();

                    string s = _previewWindowXaml;
                    s = s.Replace("@@TITLE", title.Replace("'", "&apos;"));

                    using (var reader = new System.Xml.XmlTextReader(new StringReader(s)))
                    {
                        Window preview = System.Windows.Markup.XamlReader.Load(reader) as Window;

                        DocumentViewer dv1 = LogicalTreeHelper.FindLogicalNode(preview, "dv1") as DocumentViewer;
                        dv1.ApplyTemplate();
                        dv1.Document = fds as IDocumentPaginatorSource;

                        ContentControl cc = dv1.Template.FindName("PART_FindToolBarHost", dv1) as ContentControl;
                        cc.Visibility = Visibility.Collapsed;


                        preview.ShowDialog();
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch
                    {
                    }
                }
            }
        }

        public static FlowDocument CreateFlowDocument(Visual visual, Size pageSize)
        {
            FrameworkElement fe = (visual as FrameworkElement);
            fe.Measure(new Size(Int32.MaxValue, Int32.MaxValue));
            Size visualSize = fe.DesiredSize;
            //Size visualSize = new Size(fe.ActualWidth, fe.ActualHeight);
            fe.Arrange(new Rect(new Point(0, 0), visualSize));
            MemoryStream stream = new MemoryStream();
            string pack = "pack://temp.xps";
            Uri uri = new Uri(pack);
            DocumentPaginator paginator;
            XpsDocument xpsDoc;
            using (Package container = Package.Open(stream, FileMode.Create))
            {
                PackageStore.AddPackage(uri, container);
                using (xpsDoc = new XpsDocument(container, CompressionOption.Fast, pack))
                {
                    XpsSerializationManager rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                    rsm.SaveAsXaml(visual);
                    paginator = ((IDocumentPaginatorSource)xpsDoc.GetFixedDocumentSequence()).DocumentPaginator;
                    paginator.PageSize = visualSize; // new Size(1000, 5000);
                }
                PackageStore.RemovePackage(uri);
            }
            using (Package container = Package.Open(stream, FileMode.Create))
            {
                using (xpsDoc = new XpsDocument(container, CompressionOption.Fast, pack))
                {
                    paginator = new VisualDocumentPaginator(paginator, new Size(pageSize.Width, pageSize.Height), new Size(48, 48));
                    XpsSerializationManager rsm = new XpsSerializationManager(new XpsPackagingPolicy(xpsDoc), false);
                    rsm.SaveAsXaml(paginator);
                }
                PackageStore.RemovePackage(uri);
            }

            FlowDocument document = ConvertXPSDocumentToFlowDocument(stream);
            stream.Close();
            return document;
        }

        public static string GetFileName(Uri uri)
        {
            if (!uri.IsAbsoluteUri)
            {
                string[] chunks = uri.OriginalString.Split('/');
                return chunks[chunks.Length - 1];
            }
            else
            {
                return uri.Segments[uri.Segments.Length - 1];
            }
        }

        private static void DeobfuscateData(byte[] fontData, string guid)
        {
            byte[] guidBytes = new byte[16];
            for (int i = 0; i < guidBytes.Length; i++)
            {
                guidBytes[i] = Convert.ToByte(guid.Substring(i*2, 2), 16);
            }
            for (int i = 0; i < 32; i++)
            {
                int gi = guidBytes.Length - (i%guidBytes.Length) - 1;
                fontData[i] ^= guidBytes[gi];
            }
        }

        //private static void DeobfuscateData(byte[] fontData, Guid guid)
        //{
        //    byte[] buffer = guid.ToByteArray();

        //    for (int j = 0; j < 2; ++j)
        //    {
        //        for (int i = 0; i < 16; ++i)
        //        {
        //            fontData[i + (j * 16)] = (byte)(fontData[i + (j * 16)] ^ buffer[15 - i]);
        //        }
        //    }
        //}

        public static void SaveToDisk(XpsFont font, string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            using (Stream stm = font.GetStream())
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    byte[] dta = new byte[stm.Length];
                    stm.Read(dta, 0, dta.Length);
                    if (font.IsObfuscated)
                    {
                        string guid = new Guid(GetFileName(font.Uri).Split('.')[0]).ToString("N");
                        DeobfuscateData(dta, guid);
                    }
                    fs.Write(dta, 0, dta.Length);
                }
            }
        }

        //public static void SaveToDisk(XpsFont font, string path)
        //{
        //    string folder = System.IO.Path.GetDirectoryName(path);
        //    if (!Directory.Exists(folder))
        //        Directory.CreateDirectory(folder);
        //    using (Stream stm = font.GetStream())
        //    {
        //        using (FileStream fs = new FileStream(path, FileMode.Create))
        //        {
        //            byte[] dta = new byte[stm.Length];
        //            stm.Read(dta, 0, dta.Length);
        //            if (font.IsObfuscated)
        //            {
        //                string guid = new Guid(GetFileName(font.Uri).Split('.')[0]).ToString("N");
        //                DeobfuscateData(dta, new Guid(guid));
        //            }
        //            fs.Write(dta, 0, dta.Length);
        //        }
        //    }
        //}

        public static void SaveToDisk(XpsImage image, string path)
        {
            using (Stream stm = image.GetStream())
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    byte[] dta = new byte[stm.Length];
                    stm.Read(dta, 0, dta.Length);
                    fs.Write(dta, 0, dta.Length);
                }
            }
        }

        public static FlowDocument ConvertXPSDocumentToFlowDocument(Stream stream)
        {
            FlowDocument fdoc = new FlowDocument();
            fdoc.FlowDirection = FlowDirection.LeftToRight;
            Package pkg = Package.Open(stream, FileMode.Open, FileAccess.Read);
            string pack = "pack://temp.xps";
            Uri uri = new Uri(pack);
            PackageStore.AddPackage(uri, pkg);
            XpsDocument _doc = new XpsDocument(pkg, CompressionOption.Fast, pack);
            DocumentPaginator xpsPaginator = ((IDocumentPaginatorSource)_doc.GetFixedDocumentSequence()).DocumentPaginator;
            DocumentPage fixedpage = xpsPaginator.GetPage(0);
            fdoc.PageHeight = fixedpage.Size.Height;
            fdoc.PageWidth = fixedpage.Size.Width;
            fdoc.ColumnGap = 0;
            fdoc.ColumnWidth = fixedpage.Size.Width;
            fdoc.PagePadding = new Thickness(0, 0, 0, 0);
            DocumentPaginator flowPainator = ((IDocumentPaginatorSource)fdoc).DocumentPaginator;
            flowPainator.PageSize = fixedpage.Size;
            IXpsFixedDocumentSequenceReader fixedDocSeqReader = _doc.FixedDocumentSequenceReader;
            Dictionary<string, string> imageList = new Dictionary<string, string>();
            Dictionary<string, string> fontList = new Dictionary<string, string>();
            foreach (IXpsFixedDocumentReader docReader in fixedDocSeqReader.FixedDocuments)
            {
                foreach (IXpsFixedPageReader fixedPageReader in docReader.FixedPages)
                {
                    while (fixedPageReader.XmlReader.Read())
                    {
                        string page = fixedPageReader.XmlReader.ReadOuterXml();

                        foreach (XpsFont font in fixedPageReader.Fonts)
                        {
                            string name = GetFileName(font.Uri);
                            string guid = new Guid(GetFileName(font.Uri).Split('.')[0]).ToString("N");
                            name = System.IO.Path.Combine(guid, System.IO.Path.GetFileNameWithoutExtension(name) + ".ttf");
                            string path = string.Format(@"{0}\{1}", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name);
                            if (!fontList.ContainsKey(font.Uri.OriginalString))
                            {
                                SaveToDisk(font, path);
                                fontList.Add(font.Uri.OriginalString, path);
                            }
                        }

                        foreach (XpsImage image in fixedPageReader.Images)
                        {
                            //here to get images
                            string name = GetFileName(image.Uri);
                            string path = string.Format(@"{0}\{1}", System.IO.Path.GetTempPath(), name);

                            if (!imageList.ContainsKey(image.Uri.OriginalString))
                            {
                                imageList.Add(image.Uri.OriginalString, path);
                                SaveToDisk(image, path);
                            }
                        }

                        foreach (KeyValuePair<string, string> val in fontList)
                        {
                            page = ReplaceAttribute(page, "FontUri", val.Key, val.Value);
                        }
                        foreach (KeyValuePair<string, string> val in imageList)
                        {
                            page = ReplaceAttribute(page, "ImageSource", val.Key, val.Value);
                        }

                        FixedPage fp = XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(page))) as FixedPage;
                        foreach (UIElement ui in fp.Children)
                        {
                            if (ui is Glyphs)
                            {
                                Glyphs glyph = (Glyphs)ui;
                                System.Windows.Data.Binding b = new Binding();
                                b.Source = glyph;
                                b.Path = new PropertyPath(Glyphs.UnicodeStringProperty);
                                glyph.SetBinding(TextSearch.TextProperty, b);
                            }
                        }

                        BlockUIContainer cont = new BlockUIContainer();
                        cont.Child = fp;
                        ((Block)cont).Margin = new Thickness(0);
                        ((Block)cont).Padding = new Thickness(0);
                        fdoc.Blocks.Add(cont);
                    }
                }
            }
            pkg.Close();
            PackageStore.RemovePackage(uri);
            return fdoc;
        }

        public static string StipAttributes(string srs, params string[] attributes)
        {
            return System.Text.RegularExpressions.Regex.Replace(srs,
                string.Format(@"{0}(?:\s*=\s*(""[^""]*""|[^\s>]*))?",
                string.Join("|", attributes)),
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        public static string ReplaceAttribute(string srs, string attributeName, string replacementValue)
        {
            return System.Text.RegularExpressions.Regex.Replace(srs,
                string.Format(@"{0}(?:\s*=\s*(""[^""]*""|[^\s>]*))?", attributeName),
                string.Format("{0}=\"{1}\"", attributeName, replacementValue),
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        public static string ReplaceAttribute(string srs, string attributeName, string attributeValue, string replacementValue)
        {
            return srs.Replace(attributeValue, replacementValue);
        }

    }

    public class VisualDocumentPaginator : DocumentPaginator
    {
	    Size m_PageSize;
	    Size m_Margin;
	    DocumentPaginator m_Paginator = null;
	    Size m_ContentSize;
	    ContainerVisual m_PageContent;
	    ContainerVisual m_SmallerPage;
	    ContainerVisual m_SmallerPageContainer;
	    ContainerVisual m_NewPage;

        private int _pagesHigh;
        private int _pagesWide;
	
	    public VisualDocumentPaginator(DocumentPaginator paginator,
		       Size pageSize, Size margin)
	    {
		    m_PageSize = pageSize;
		    m_Margin = margin;
		    m_Paginator = paginator;
		    m_ContentSize = new Size(pageSize.Width - 2 * margin.Width,
								     pageSize.Height - 2 * margin.Height);

            _pagesHigh = (int)Math.Ceiling(m_Paginator.PageSize.Height / m_ContentSize.Height);
            _pagesWide = (int)Math.Ceiling(m_Paginator.PageSize.Width / m_ContentSize.Width);

		    m_Paginator.PageSize = m_ContentSize;
		    m_PageContent = new ContainerVisual();
		    m_SmallerPage = new ContainerVisual();
		    m_NewPage = new ContainerVisual();
		    m_SmallerPageContainer = new ContainerVisual();
	    }

	    Rect Move(Rect rect)
	    {
		    if (rect.IsEmpty)
		    {
			    return rect;
		    }
		    else
		    {
			    return new Rect(rect.Left + m_Margin.Width,
							    rect.Top + m_Margin.Height,
							    rect.Width, rect.Height);
		    }
	    }

	    public override DocumentPage GetPage(int pageNumber)
	    {
		    m_PageContent.Children.Clear();
		    m_SmallerPage.Children.Clear();
		    m_NewPage.Children.Clear();
		    m_SmallerPageContainer.Children.Clear();
		    DrawingVisual title = new DrawingVisual();
		    using (DrawingContext ctx = title.RenderOpen())
		    {
			    FontFamily font = new FontFamily("Times New Roman");
			    Typeface typeface =
			      new Typeface(font, FontStyles.Normal,
						       FontWeights.Bold, FontStretches.Normal);
			    FormattedText text = new FormattedText("Page " +
				    (pageNumber + 1) + " of " + PageCount,
				    System.Globalization.CultureInfo.CurrentCulture,
				    FlowDirection.LeftToRight,
				    typeface, 14, Brushes.Black);
			    ctx.DrawText(text, new Point(0, 0));
		    }
 
		    DocumentPage page = m_Paginator.GetPage(0);
		    m_PageContent.Children.Add(page.Visual);

	        var left = m_ContentSize.Width*(pageNumber%_pagesWide);
	        var top = m_ContentSize.Height*(pageNumber%_pagesHigh);

		    var clip = new RectangleGeometry(new Rect(left, top, m_ContentSize.Width, m_ContentSize.Height));

		    m_PageContent.Clip = clip;
		    m_PageContent.Transform = new TranslateTransform(-left, -top);

		    m_SmallerPage.Children.Add(m_PageContent);
		    m_SmallerPage.Transform = new ScaleTransform(0.95,0.95);
		    m_SmallerPageContainer.Children.Add(m_SmallerPage);
		    m_SmallerPageContainer.Transform = new TranslateTransform(0, 24);
		    m_NewPage.Children.Add(title);
		    m_NewPage.Children.Add(m_SmallerPageContainer);
		    m_NewPage.Transform =
				      new TranslateTransform(m_Margin.Width, m_Margin.Height);
		    return new DocumentPage(m_NewPage, m_PageSize, 
				       Move(page.BleedBox),Move(page.ContentBox));
	    }

	    public override bool IsPageCountValid
	    {
		    get
		    {
			    return true;
		    }
	    }

	    public override int PageCount
	    {
		    get
		    {
			    return _pagesHigh * _pagesWide;
		    }
	    }

	    public override Size PageSize
	    {
		    get
		    {
			    return m_Paginator.PageSize;
		    }
		    set
		    {
			    m_Paginator.PageSize = value;
		    }
	    }

	    public override IDocumentPaginatorSource Source
	    {
		    get
		    {
			    if (m_Paginator != null)
				    return m_Paginator.Source;
			    return null;
		    }
	    }
     }



}
