using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class FamilyViewViewer : UserControl, INotifyPropertyChanged
    {
        public PeopleCollection Family { get; set; }

        public FamilyViewViewer()
        {
            InitializeComponent();

            DataContext = this;

            addChildBtn = new Button();
            addChildBtn.Content = "Add";
            addChildBtn.ToolTip = "Add child";
            addChildBtn.Click += addChild_Click;

            Family = App.Family;
            Family.CurrentChanged += Family_CurrentChanged;
            Family_CurrentChanged(null, null);
        }

        private SpouseRelationship spouse;

        void Family_CurrentChanged(object sender, EventArgs e)
        {
            dad.Human = Family.Current;
            spouse = null;

            // go for first spouse
            foreach (Relationship rel in dad.Human.Relationships)
            {
                if (rel.RelationshipType == RelationshipType.Spouse)
                {
                    spouse = rel as SpouseRelationship;
                    break;
                }
            }

            OnPropertyChanged("MarrDate");
            OnPropertyChanged("MarrPlace");
            OnPropertyChanged("DivDate");
            OnPropertyChanged("DivPlace");

            mum.Human = spouse != null ? spouse.RelationTo : null;

            MakeBabies();
        }

        private List<PersonView> childViews = new List<PersonView>();
        private Button addChildBtn;

        void MakeBabies()
        {
            // TODO children and multiple spouses

            ChildRow.Children.Clear();
            childViews.Clear();

            var childs = Family.Current.Children;
            foreach (var person in childs)
            {
                PersonView aChildView = new PersonView();
                aChildView.Human = person;
                aChildView.Child = true;

                ChildRow.Children.Add(aChildView);
            }

            ChildRow.Children.Add(addChildBtn);
        }

        // Does 'dad' have more than one spouse?
        public bool DadHasMoreSpouse
        {
            get { return dad.Human != null && dad.Human.Spouses.Count > 1; }
        }

        // Does 'mom' have more than one spouse?
        public bool MumHasMoreSpouse
        {
            get { return mum.Human != null && mum.Human.Spouses.Count > 1; }
        }

        private void addChild_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void dadParents_Click(object sender, RoutedEventArgs e)
        {

        }

        private void addDadParents_Click(object sender, RoutedEventArgs e)
        {

        }

        private void mumParents_Click(object sender, RoutedEventArgs e)
        {
        }

        private void addMumParents_Click(object sender, RoutedEventArgs e)
        {
        }

        private void dadSpouses_Click(object sender, RoutedEventArgs e)
        {
        }

        private void mumSpouses_Click(object sender, RoutedEventArgs e)
        {
        }

        private void addDadSpouse_Click(object sender, RoutedEventArgs e)
        {
        }

        private void addMumSpouse_Click(object sender, RoutedEventArgs e)
        {
        }

        private void doTooltip(object sender, string format, string param )
        {
            var b = sender as FrameworkElement;
            if (b == null)
                return;
            var t = b.ToolTip as string;
            if (t == null)
                return;
            b.ToolTip = String.Format(format, param);
        }

        private void addDadSpouse_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (dad.Human != null)
                doTooltip(sender, "Add new spouse for {0}", dad.Human.FullName);
        }

        private void dadSpouses_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (dad.Human == null)
                return;

            if (DadHasMoreSpouse)
            {
                doTooltip(sender, "View other spouses for {0}", dad.Human.FullName);
            }
            else
            {
                doTooltip(sender, "{0} has no other spouses", dad.Human.FullName);
            }
        }

        private void addMumSpouse_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            doTooltip(sender, "Add new spouse for {0}", mum.Human.FullName);
        }

        private void mumSpouses_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (MumHasMoreSpouse)
            {
                doTooltip(sender, "View other spouses for {0}", mum.Human.FullName);
            }
            else
            {
                doTooltip(sender, "{0} has no other spouses", mum.Human.FullName);
            }
        }

        public DateTime? MarrDate
        {
            get
            {
                return spouse == null ? null : spouse.MarriageDate;
            }
        }

        public string MarrPlace { get { return spouse == null ? "" : spouse.MarriagePlace; } }

        public DateTime? DivDate
        {
            get
            {
                return spouse == null ? null : spouse.DivorceDate;
            }
        }

        public string DivPlace { get { return ""; } } // TODO use DivorcePlace when added

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
