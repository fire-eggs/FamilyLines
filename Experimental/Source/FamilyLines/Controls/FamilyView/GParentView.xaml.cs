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
                return Father ? "Add Father" : "Add Mother";
            }
        }

        public string ChildName { get; set; }

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
            // TODO invoke 'Add person' with appropriate value
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
            doTooltip(sender, Father ? "Add father for {0}" : 
                                       "Add mother for {0}", ChildName);
        }

        private void Button_ToolTipOpening_1(object sender, System.Windows.Controls.ToolTipEventArgs e)
        {
            doTooltip(sender, "Goto {0}", Human.FullName);
        }

    }
}
