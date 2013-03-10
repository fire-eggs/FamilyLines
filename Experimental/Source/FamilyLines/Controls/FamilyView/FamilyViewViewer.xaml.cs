using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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
            if (dad.Human == null)
                return;

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

            mum.Human = spouse != null ? spouse.RelationTo : null;
            IsMarried = spouse != null;
            IsDivorced = spouse != null && spouse.DivorceDate != null;

            // TODO why isn't databinding working???
            mum.Visibility = IsMarried ? Visibility.Visible : Visibility.Collapsed;

            MakeBabies();

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
        }

        private void addChild_Click(object sender, RoutedEventArgs e)
        {
            // Add a child to this marriage
            // TODO: need a "add child out of wedlock" mechanism?
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

        private void DelMarr_OnClick(object sender, RoutedEventArgs e)
        {
        }

        // TODO means to view children out of wedlock?
    }
}
