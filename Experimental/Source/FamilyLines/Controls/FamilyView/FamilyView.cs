using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    class FamilyView : Canvas
    {
        private Person primaryPerson;

        // Note: load or import will change the family
        public PeopleCollection Family { get; set; }

        public FamilyView()
        {
            // The list of people, this is a global list shared by the application.
            Family = App.Family;

            // TODO add the ContentChanged, CurrentChanged handlers
            Family.ContentChanged += Family_ContentChanged;
            Family.CurrentChanged += Family_CurrentChanged;

            // Be aware when the family changes on load/import
            App.FamilyCollection.PeopleCollectionChanged += FamilyCollection_PeopleCollectionChanged;
        }

        private void updateDiagram()
        {
            // This is sort of a treemap thing. 
            /*
             * +----------+----------+
             * +----------+----------+
            */

            clear();
            topRow();
            botRow();

            InvalidateVisual();
            InvalidateArrange();
            InvalidateMeasure();

        }

        // Reset all data associated to view
        private void clear()
        {
            //foreach (var rectangle in squares)
            //{
            //    RemoveVisualChild(rectangle);
            //}
            //squares.Clear();
        }

        private void topRow()
        {
            // get the primary person
            primaryPerson = Family.Current;

            // how many spouses?
            int spiceCount = primaryPerson.Spouses.Count;

            switch (spiceCount)
            {
                case 0:
                case 1:
                    simpleTopRow();
                    break;
                //case 2:
                //    mediumTopRow();
                //    break;
                default:
                    puntTopRow();
                    break;
            }
        }

        private void botRow()
        {
            
        }

        // The primary person has 0/1 spouse
        private void simpleTopRow()
        {
            makeRect(0,0,200,200,Brushes.Purple, Brushes.GreenYellow, "blah", primaryPerson);

            Person spouse = null;
            if (primaryPerson.Spouses.Count > 0)
                spouse = primaryPerson.Spouses[0];
            makeRect(200, 0, 200, 200, Brushes.CadetBlue, Brushes.BurlyWood, "blah", spouse);

        }

        private void puntTopRow()
        {
            
        }

        private void FamilyCollection_PeopleCollectionChanged(object sender, EventArgs e)
        {
            Family = App.FamilyCollection.PeopleCollection;
            // release previous handlers for garbage collector
            Family.ContentChanged -= Family_ContentChanged;
            Family.CurrentChanged -= Family_CurrentChanged;

            // Get the new family data
            Family = App.FamilyCollection.PeopleCollection;
            Family.ContentChanged += Family_ContentChanged;
            Family.CurrentChanged += Family_CurrentChanged;

            // Force a rebuild of the diagram
            Family_ContentChanged(null, new ContentChangedEventArgs(null));
        }

        void Family_CurrentChanged(object sender, EventArgs e)
        {
            updateDiagram();
        }

        void Family_ContentChanged(object sender, ContentChangedEventArgs e)
        {
            updateDiagram();
        }

        private void makeRect(int x, int y, int h, int w, Brush color, Brush fill, string ttip, object dc)
        {
            var r = new Rectangle();
            r.Height = h;
            r.Width = w;
            r.StrokeThickness = 2;
            r.Stroke = color;
            r.Fill = fill;
            r.ToolTip = ttip;
            r.IsHitTestVisible = true;
//            r.Cursor = Magnify;
            r.DataContext = dc;
//            r.MouseLeftButtonDown += spaceMouseClick;

            Children.Add(r);
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
        }

    }
}
