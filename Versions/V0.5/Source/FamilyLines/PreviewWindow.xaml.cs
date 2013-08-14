#region File Info
// File       : PreviewWindow.xaml.cs
// Description: 
// Package    : VisualPrint
//
// Authors    : Fred Song
//
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.IO;
using System.IO.Packaging;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Xps.Serialization;

namespace VisualPrint
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PreviewWindow : Window
    {
        public PreviewWindow(FlowDocument fdoc)
        {
            InitializeComponent();
            Viewer.Document = fdoc;
            Viewer.ViewingMode = FlowDocumentReaderViewingMode.Scroll;
        }

        #region RoutedEvents

        public static readonly RoutedEvent PrintEvent = EventManager.RegisterRoutedEvent(
            "Print", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PreviewWindow));

        public event RoutedEventHandler Print
        {
            add { AddHandler(PrintEvent, value); }
            remove { RemoveHandler(PrintEvent, value); }
        }

        public FlowDocument Document { get { return Viewer.Document; } }

        #endregion

        private void OnPrintClick(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(PrintEvent));
            Close();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            Viewer.Document = null;
            //make sure the GC collects everything
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
