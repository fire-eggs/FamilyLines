using System.Windows.Documents;

namespace KBS.FamilyLines
{
    /// <summary>
    /// Interaction logic for PrintPreviewWindow.xaml
    /// </summary>
    public partial class PrintPreviewWindow
    {
        public PrintPreviewWindow(IDocumentPaginatorSource document)
        {
            InitializeComponent();
            DataContext = document;
        }
    }
}
