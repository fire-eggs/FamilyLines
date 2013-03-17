using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    /// <summary>
    /// Interaction logic for FamilyViewViewer.xaml
    /// </summary>
    public partial class FamilyViewViewer : UserControl, INotifyPropertyChanged
    {
        public PeopleCollection Family { get; set; }

        public bool IsMarried { get; set; }
        public bool IsDivorced { get; set; }

        public FamilyViewViewer()
        {
            InitializeComponent();

            DataContext = this; // TODO set in XAML
        }

        public void Init()
        {
            Family = App.Family;
            Family.CurrentChanged += Family_CurrentChanged;
            Family_CurrentChanged(null, null);
            Family.ContentChanged += OnFamilyContentChanged;
        }

        private void OnFamilyContentChanged(object sender, ContentChangedEventArgs e)
        {
            Family_CurrentChanged(null, null);
        }

        private SpouseRelationship spouse;

        void Family_CurrentChanged(object sender, EventArgs e)
        {
            dad.Human = Family.Current;
            spouse = null;
            if (dad.Human == null)
            {
                GDad1.Human = null;
                GMum1.Human = null;
                GDad2.Human = null;
                GMum2.Human = null;
                return;
            }

            // go for first spouse
            foreach (Relationship rel in dad.Human.Relationships)
            {
                if (rel.RelationshipType == RelationshipType.Spouse)
                {
                    spouse = rel as SpouseRelationship;
                    break;
                }
            }

            mum.Human = spouse != null ? spouse.RelationTo : null;
            IsMarried = spouse != null;
            IsDivorced = spouse != null && spouse.DivorceDate != null;

            // TODO why isn't databinding working???
            mum.Visibility = IsMarried ? Visibility.Visible : Visibility.Collapsed;

            MakeBabies();
            UpdateGParents();

            OnPropertyChanged("IsMarried");
            OnPropertyChanged("IsDivorced");
            OnPropertyChanged("MarrDate");
            OnPropertyChanged("MarrPlace");
            OnPropertyChanged("DivDate");
            OnPropertyChanged("DivPlace");
        }

        private List<PersonView> childViews = new List<PersonView>();

        void MakeBabies()
        {
            // TODO children and multiple spouses [i.e. this code uses all children of person; need only those children from this marriage]
            int num = ChildRow.Children.Count;
            ChildRow.Children.RemoveRange(1,num-1);

//            ChildRow.Children.Clear();
            childViews.Clear();

            var childs = Family.Current.Children;
            foreach (var person in childs)
            {
                PersonView aChildView = new PersonView();
                aChildView.Human = person;
                aChildView.Child = true;

                ChildRow.Children.Add(aChildView);
            }
        }

        public DateTime? MarrDate
        {
            get
            {
                return spouse == null ? null : spouse.MarriageDate;
                // TODO show '?' when null date?
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

        private void UpdateGParents()
        {
            GDad1.Human = dad.Human.Parents.Count > 0 ? dad.Human.Parents[0] : null;
            GMum1.Human = dad.Human.Parents.Count > 1 ? dad.Human.Parents[1] : null;

            GDad1.Child = GMum1.Child = dad.Human;

            if (mum.Human != null)
            {
                GDad2.Human = mum.Human.Parents.Count > 0 ? mum.Human.Parents[0] : null;
                GMum2.Human = mum.Human.Parents.Count > 1 ? mum.Human.Parents[1] : null;
                GDad2.Child = GMum2.Child = mum.Human;
            }
            else
            {
                GDad2.Human = GMum2.Human = null;
                GDad2.Child = GMum2.Child = null;
            }
        }

        private void Marriage_Click(object sender, MouseButtonEventArgs e)
        {
            // Fire an 'edit marriage' event
            var e2 = new RoutedEventArgs(Commands.EditMarriage, mum.Human);
            RaiseEvent(e2);
        }

        private void AddSon_Click(object sender, RoutedEventArgs e)
        {
            // Fire an 'Add Child' event
            var e2 = new RoutedEventArgs(Commands.AddChild, Properties.Resources.Son);
            RaiseEvent(e2);
        }

        private void AddDau_Click(object sender, RoutedEventArgs e)
        {
            // Fire an 'Add Child' event
            var e2 = new RoutedEventArgs(Commands.AddChild, Properties.Resources.Daughter);
            RaiseEvent(e2);
        }
    }
}
