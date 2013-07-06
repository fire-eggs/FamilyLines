/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    /// <summary>
    /// Interaction logic for FamilyViewViewer.xaml
    /// </summary>
    public partial class FamilyViewViewer : INotifyPropertyChanged
    {
        private SpouseRelationship spouse;
        private readonly List<PersonView> childViews = new List<PersonView>();

        public PeopleCollection Family { get; set; }

        public bool IsMarried { get; set; }
        public bool IsDivorced { get; set; }

        public FamilyViewViewer()
        {
            InitializeComponent();

            DataContext = this; // TODO set in XAML

            AddViewSpouseH += FamilyViewViewer_AddViewSpouseH;
        }

        public event RoutedEventHandler AddViewSpouseH
        {
            add
            {
                AddHandler(Commands.ViewSpouse, value);
            }
            remove
            {
                RemoveHandler(Commands.ViewSpouse, value);
            }
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

        void MakeBabies()
        {
            // TODO children and multiple spouses [i.e. this code uses all children of person; need only those children from this marriage]

            // Wipe previous family's children. Relying on the first entry being the "add child" placeholder.
            int num = ChildRow.Children.Count;
            ChildRow.Children.RemoveRange(1,num-1);
            childViews.Clear();

            // Get children of the current marriage TODO should this be a method on Person?
            var childs = dad.Human.Children;
            foreach (var person in childs)
            {
                // "first" parent could be mum or dad
                if (person.Parents[0] != dad.Human &&
                    person.Parents[0] != mum.Human)
                    continue;
                if (person.Parents.Count > 1 &&
                    person.Parents[1] != mum.Human &&
                    person.Parents[1] != dad.Human)
                    continue;

                var aChildView = new PersonView();
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

        public string DivPlace { get { return spouse == null ? "" : spouse.DivorcePlace; } }

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

                // TODO databinding in GParentView
                GDad2.Visibility = Visibility.Visible;
                GMum2.Visibility = Visibility.Visible;
            }
            else
            {
                GDad2.Human = GMum2.Human = null;
                GDad2.Child = GMum2.Child = null;

                // TODO databinding in GParentView
                GDad2.Visibility = Visibility.Collapsed;
                GMum2.Visibility = Visibility.Collapsed;
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
            var childProps = new Tuple<Person, FamilyMemberComboBoxValue>(dad.Human, FamilyMemberComboBoxValue.Son);
            var e2 = new RoutedEventArgs(Commands.AddChild, childProps);
            RaiseEvent(e2);
        }

        private void AddDau_Click(object sender, RoutedEventArgs e)
        {
            // Fire an 'Add Child' event
            var childProps = new Tuple<Person, FamilyMemberComboBoxValue>(dad.Human, FamilyMemberComboBoxValue.Daughter);
            var e2 = new RoutedEventArgs(Commands.AddChild, childProps);
            RaiseEvent(e2);
        }

        private void FamilyViewViewer_AddViewSpouseH(object sender, RoutedEventArgs e)
        {
            // User has requested to view a different spouse for one of the 'parents'
            // TODO This isn't quite 'kosher': the OriginalSource property has been set up with the event properties
            var spouseProps = e.OriginalSource as Tuple<Person, Person>;

            // Determine which person to change
            var pView = mum; // assume we're asking to see a different spouse of 'dad'
            var sView = dad;
            if (spouseProps.Item1 == mum.Human) // see if the requester is 'mom'
            {
                pView = dad;
                // TODO: should we make this new person the 'current' person?                
                sView = mum;
            }

            // TODO very much brute force can this be improved? DRY

            // Update the 'spouse' pointer
            pView.Human = spouseProps.Item2;
            foreach (Relationship rel in sView.Human.Relationships)
            {
                if (rel.RelationshipType == RelationshipType.Spouse &&
                    rel.RelationTo == pView.Human)
                {
                    spouse = rel as SpouseRelationship;
                    break;
                }
            }

            IsMarried = spouse != null;
            IsDivorced = spouse != null && spouse.DivorceDate != null;

            MakeBabies();
            UpdateGParents();

            OnPropertyChanged("IsMarried");
            OnPropertyChanged("IsDivorced");
            OnPropertyChanged("MarrDate");
            OnPropertyChanged("MarrPlace");
            OnPropertyChanged("DivDate");
            OnPropertyChanged("DivPlace");

        }

        private void AddSpouse_Click(object sender, RoutedEventArgs e)
        {
            // Fire an 'add spouse' event
            var e2 = new RoutedEventArgs(Commands.AddSpouse, dad.Human);
            RaiseEvent(e2);
        }

    }
}
