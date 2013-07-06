/*
 * Family Lines code is provided using the Apache License V2.0, January 2004 http://www.apache.org/licenses/
 * 
 */
using System;
using System.ComponentModel;
using System.Windows;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    /// <summary>
    /// Interaction logic for GParentView.xaml
    /// </summary>
    public partial class GParentView : INotifyPropertyChanged
    {
        #region Properties

        private Person _human;
        public Person Human
        {
            get
            {
                return _human;
            }
            set
            {
                _human = value;

                OnPropertyChanged("Human");
                OnPropertyChanged("Show");
            }
        }

        public bool Father { get; set; }

        public bool Show { get { return _human != null; } }

        public string TypeName
        {
            get
            {
                return Father ? Properties.Resources.AddFather : Properties.Resources.GParentView_Add_Mother;
            }
        }

        public Person Child { get; set; }

        #endregion

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public GParentView()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void go_click(object sender, RoutedEventArgs e)
        {
            App.Family.Current = _human;
        }

        private void add_click(object sender, RoutedEventArgs e)
        {
            // Fire 'Add person' event
            var parentProps = new Tuple<FamilyMemberComboBoxValue, Person>(
                Father ? FamilyMemberComboBoxValue.Father : FamilyMemberComboBoxValue.Mother,
                Child);
            var e2 = new RoutedEventArgs(Commands.AddParent, parentProps);
            RaiseEvent(e2);
        }

        private void doTooltip(object sender, string format, string param)
        {
            var b = sender as FrameworkElement;
            if (b == null)
                return;
            var t = b.ToolTip as string;
            if (t == null)
                return;
            b.ToolTip = string.Format(format, param);
        }

        private void Button_ToolTipOpening(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            doTooltip(sender, Father ? Properties.Resources.GParentView_Add_father_for_ : 
                                       Properties.Resources.GParentView_Add_mother_for, 
                                       Child.FullName);
        }

        private void Button_ToolTipOpening_1(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            doTooltip(sender, Properties.Resources.GParentView_Change_the_current_person, Human.FullName);
        }

    }
}
