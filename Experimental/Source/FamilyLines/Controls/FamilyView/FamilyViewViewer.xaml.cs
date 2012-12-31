using System;
using System.Collections.Generic;
using System.Linq;
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
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    /// <summary>
    /// Interaction logic for FamilyViewViewer.xaml
    /// </summary>
    public partial class FamilyViewViewer : UserControl
    {
        public PeopleCollection Family { get; set; }

        public FamilyViewViewer()
        {
            InitializeComponent();

            Family = App.Family;

            Family.CurrentChanged += Family_CurrentChanged;

        }

        void Family_CurrentChanged(object sender, EventArgs e)
        {
            dad.Human = Family.Current;

            if (Family.Current.Spouses.Count < 1)
                mum.Human = null;
            else
            {
                mum.Human = Family.Current.Spouses[0];
            }
        }

    }
}
