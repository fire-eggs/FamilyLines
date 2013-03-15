using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    /// <summary>
    /// Interaction logic for PersonView.xaml
    /// </summary>
    public partial class PersonView : INotifyPropertyChanged
    {
        #region Properties

        private Person _human;
        public Person Human
        {
            get { return _human; }
            set
            {
                _human = value;

                UpdateSpouseList();

                OnPropertyChanged("Human");
                OnPropertyChanged("HasMoreSpouse");
            }
        }

        public bool Child { get; set; }

        public int SpouseColumn { set; get; }

        public bool HasMoreSpouse
        {
            get
            {
                return _human != null && _human.Spouses.Count > 1;
            }
        }
        #endregion

        public PersonView()
        {
            InitializeComponent();

            // TODO attempts to set datacontext in XAML have been spotty...
            DataContext = this;
            SpouseNav.DataContext = this;

            SpouseColumn = 0;
        }

        private void UpdateSpouseList()
        {
            SpouseList.Items.Clear();
            if (Human == null)
                return;

            // 1. Create 'Add new spouse' entry
            var cbi = new ComboBoxItem();
            cbi.Content = "Add a new spouse";
            cbi.Selected += addSpouse_Selected;
            SpouseList.Items.Add(cbi);

            // 2. For each spouse, add an entry
            foreach (Relationship rel in Human.Relationships)
            {
                if (rel.RelationshipType == RelationshipType.Spouse)
                {
                    var spouse = rel as SpouseRelationship;
                    if (spouse == null)
                        continue;
                    var cbiS = new ComboBoxItem();
                    cbiS.Content = spouse.PersonFullName;
                    cbiS.DataContext = spouse;
                    cbiS.Selected += gotoSpouse_Selected;

                    SpouseList.Items.Add(cbiS);
                }
            }
        }

        private void addSpouse_Selected(object sender, RoutedEventArgs e)
        {
            // TODO fire an 'add spouse' event
        }

        private void gotoSpouse_Selected(object sender, RoutedEventArgs e)
        {
            // TODO invoke 'go' on sender.DataContext
        }

        private void doTooltip(object sender, string format, string param)
        {
            var b = sender as FrameworkElement;
            if (b == null)
                return;
            var t = b.ToolTip as string;
            if (t == null)
                return;
            b.ToolTip = String.Format(format, param);
        }

        private void go(Person aperson)
        {
            App.Family.Current = aperson;
        }

        #region Event Handlers

        private void Border_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Human != null)
                go(Human);
        }

        private void Border_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (Human != null)
                doTooltip(sender, "Click to make {0} the current person", Human.FullName);
        }

        private void SpouseList_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (Human != null)
                doTooltip(sender, "View or add spouse for {0}", Human.FullName);
        }

        #endregion

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
