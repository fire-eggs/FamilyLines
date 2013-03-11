using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    // KBR HACK!!!!
    //public class ComposingConverter : IValueConverter
    //{
    //    #region IValueCOnverter Members

    //    private List<IValueConverter> converters = new List<IValueConverter>();

    //    public Collection<IValueConverter> Converters
    //    {
    //        get { return new Collection<IValueConverter>(this.converters); }
    //    }

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        for (int i = 0; i < this.converters.Count; i++)
    //        {
    //            value = converters[i].Convert(value, targetType, parameter, culture);
    //        }
    //        return value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        for (int i = this.converters.Count - 1; i >= 0; i--)
    //        {
    //            value = converters[i].ConvertBack(value, targetType, parameter, culture);
    //        }
    //        return value;
    //    }

    //    #endregion
    //}

    //// KBR HACK!!!!
    //public class ImageConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        var filePath = value as string;
    //        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
    //        {
    //            try
    //            {
    //                BitmapImage bitmap = new BitmapImage();
    //                bitmap.BeginInit();
    //                bitmap.CacheOption = BitmapCacheOption.OnLoad;

    //                // To save significant application memory, set the DecodePixelWidth or  
    //                // DecodePixelHeight of the BitmapImage value of the image source to the desired 
    //                // height or width of the rendered image. If you don't do this, the application will 
    //                // cache the image as though it were rendered as its normal size rather then just 
    //                // the size that is displayed.
    //                // Note: In order to preserve aspect ratio, set DecodePixelWidth
    //                // or DecodePixelHeight but not both.
    //                // See http://msdn.microsoft.com/en-us/library/ms748873.aspx
    //                bitmap.DecodePixelWidth = 200;
    //                bitmap.UriSource = new Uri(filePath);
    //                bitmap.EndInit();

    //                return bitmap;
    //            }
    //            catch
    //            {
    //                return null;
    //            }
    //        }

    //        return null;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException(Properties.Resources.NotImplemented);
    //    }

    //    #endregion
    //}

    //// KBR HACK!!!
    //public class NotConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        return !(bool)value;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException(Properties.Resources.NotImplemented);
    //    }

    //    #endregion
    //}

    //// KBR HACK!!!
    //public class BoolToVisibilityConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        if ((bool)value)
    //            return Visibility.Visible;
    //        else
    //            return Visibility.Collapsed;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException(Properties.Resources.NotImplemented);
    //    }

    //    #endregion
    //}

    //// KBR HACK!!!
    //public class DateFormattingConverter : IValueConverter
    //{
    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {

    //        if (value != null)
    //            return ((DateTime)value).ToShortDateString();

    //        return "?";
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    //    {

    //        if (string.IsNullOrEmpty((string)value))
    //            return null;

    //        string dateString = (string)value;

    //        // Append first month and day if just the year was entered
    //        if (dateString.Length == 4)
    //            dateString = "1/1/" + dateString;


    //        DateTime date;
    //        DateTime.TryParse(dateString, out date);
    //        return date;
    //    }

    //    #endregion
    //}
    
    /// <summary>
    /// Interaction logic for PersonView.xaml
    /// </summary>
    public partial class PersonView : INotifyPropertyChanged
    {
        public PersonView()
        {
            InitializeComponent();

            // TODO attempts to set datacontext in XAML have been spotty...
            DataContext = this;
            goBtn.DataContext = this;

            SpouseColumn = 0;
        }

        private Person _human;
        public Person Human
        {
            get { return _human; }
            set
            {
                _human = value;

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

        private void editbtn_Click(object sender, RoutedEventArgs e)
        {
            // TODO editing feature?
        }

        private void gobtn_Click(object sender, RoutedEventArgs e)
        {
            if (Human != null)
                go(Human);
        }

        private void parents_Click(object sender, RoutedEventArgs e)
        {
            var parent = Human.Parents[0];
            go(parent);
        }

        private void addParents_Click(object sender, RoutedEventArgs e)
        {
            // TODO invoke "add relationship"?
        }

        private void Spouses_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (HasMoreSpouse)
            {
                doTooltip(sender, "View other spouses for {0}", Human.FullName);
            }
            else
            {
                doTooltip(sender, "{0} has no other spouses", Human.FullName);
            }
        }

        private void addSpouse_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (Human != null)
                doTooltip(sender, "Add new spouse for {0}", Human.FullName);
        }

        private void addSpouse_Click(object sender, RoutedEventArgs e)
        {
            // TODO invoke "add relationship"?
        }

        // TODO: which spouse is currently visible?
        private void Spouses_Click(object sender, RoutedEventArgs e)
        {
            // 1 other spouse: go automatically
            
            // more than 1 other spouse: show a picker
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

        private void goParentDad(object sender, RoutedEventArgs e)
        {
            go(_human.Parents[0]);
        }

        private void addFather_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void goParentMum(object sender, RoutedEventArgs e)
        {
            go(_human.Parents[1]);
        }

        private void addMother_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
