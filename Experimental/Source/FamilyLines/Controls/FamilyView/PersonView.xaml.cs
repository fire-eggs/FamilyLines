using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using KBS.FamilyLines;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls.FamilyView
{
    // KBR HACK!!!!
    public class ComposingConverter : IValueConverter
    {
        #region IValueCOnverter Members

        private List<IValueConverter> converters = new List<IValueConverter>();

        public Collection<IValueConverter> Converters
        {
            get { return new Collection<IValueConverter>(this.converters); }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            for (int i = 0; i < this.converters.Count; i++)
            {
                value = converters[i].Convert(value, targetType, parameter, culture);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            for (int i = this.converters.Count - 1; i >= 0; i--)
            {
                value = converters[i].ConvertBack(value, targetType, parameter, culture);
            }
            return value;
        }

        #endregion
    }

    // KBR HACK!!!!
    public class ImageConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var filePath = value as string;
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;

                    // To save significant application memory, set the DecodePixelWidth or  
                    // DecodePixelHeight of the BitmapImage value of the image source to the desired 
                    // height or width of the rendered image. If you don't do this, the application will 
                    // cache the image as though it were rendered as its normal size rather then just 
                    // the size that is displayed.
                    // Note: In order to preserve aspect ratio, set DecodePixelWidth
                    // or DecodePixelHeight but not both.
                    // See http://msdn.microsoft.com/en-us/library/ms748873.aspx
                    bitmap.DecodePixelWidth = 200;
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.EndInit();

                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException(Properties.Resources.NotImplemented);
        }

        #endregion
    }

    // KBR HACK!!!
    public class NotConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException(Properties.Resources.NotImplemented);
        }

        #endregion
    }

    // KBR HACK!!!
    public class BoolToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return Visibility.Visible;
            else
                return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException(Properties.Resources.NotImplemented);
        }

        #endregion
    }

    // KBR HACK!!!
    public class DateFormattingConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (value != null)
                return ((DateTime)value).ToShortDateString();

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if (string.IsNullOrEmpty((string)value))
                return null;

            string dateString = (string)value;

            // Append first month and day if just the year was entered
            if (dateString.Length == 4)
                dateString = "1/1/" + dateString;


            DateTime date;
            DateTime.TryParse(dateString, out date);
            return date;
        }

        #endregion
    }
    
    /// <summary>
    /// Interaction logic for PersonView.xaml
    /// </summary>
    public partial class PersonView : UserControl
    {
        public PersonView()
        {
            InitializeComponent();
            DataContext = this;
            goBtn.DataContext = this; // TODO there must be a nicer way? in XAML?
        }

        private Person _human;
        public Person Human
        {
            get { return _human; }
            set
            {
                _human = value;
                DataContext = _human;
            }
        }

        public bool Child { get; set; }
    }
}
