/*
 * Family.Show derived code provided under MS-PL license.
 */
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About
    {
        public About()
        {
            InitializeComponent();
            DisplayVersion();
        }

        #region routed events

        [Localizable(false)] public static readonly RoutedEvent CloseButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CloseButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(About));

        // Expose this event for this control's container
        public event RoutedEventHandler CloseButtonClick
        {
            add { AddHandler(CloseButtonClickEvent, value); }
            remove { RemoveHandler(CloseButtonClickEvent, value); }
        }

        #endregion

        #region methods

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(CloseButtonClickEvent));
        }

        #endregion

        #region helper methods

        /// <summary>
        /// Display the application version.
        /// </summary>
        private void DisplayVersion()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            VersionLabel.Content += string.Format(CultureInfo.CurrentCulture, 
                "{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }

        private void Homepage_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Open the CodePlex website in the user's default browser
            try
            {
                System.Diagnostics.Process.Start("http://sourceforge.net/projects/FamilyLines/?source=directory");
            }
            catch { }
        }

        private void Discussion_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Open the CodePlex discussion website in the user's default browser
            try
            {
                System.Diagnostics.Process.Start("http://sourceforge.net/p/FamilyLines/discussion/?source=navbar");
            }
            catch { }
        }

        private void People_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Open the CodePlex people in the user's default browser
            try
            {
                System.Diagnostics.Process.Start("http://familyshow.codeplex.com/team/view");
            }
            catch { }
        }

        #endregion

    }
}