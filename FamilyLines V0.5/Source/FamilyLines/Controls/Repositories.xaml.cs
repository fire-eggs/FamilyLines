/*
 * Family.Show derived code provided under MS-PL license.
 */
using System.IO;
using System.Windows;
using KBS.FamilyLinesLib;

namespace KBS.FamilyLines.Controls
{
    /// <summary>
    /// Interaction logic for Repositories.xaml
    /// </summary>
    public partial class Repositories
    {
        public Repositories()
        {
            InitializeComponent();
        }

        #region routed events

        public static readonly RoutedEvent CancelButtonClickEvent = EventManager.RegisterRoutedEvent(
            "CancelButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Repositories));

        public event RoutedEventHandler CancelButtonClick
        {
            add { AddHandler(CancelButtonClickEvent, value); }
            remove { RemoveHandler(CancelButtonClickEvent, value); }
        }

        #endregion

        #region methods

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Save();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Add();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            RaiseEvent(new RoutedEventArgs(CancelButtonClickEvent));
        }

        private void ExportRepositoriesButton_Click(object sender, RoutedEventArgs e)
        {
            Export();           
        }

        private void RepositoriesCombobox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            Changed();
        }

        #endregion

        #region helper methods

        private void Changed()
        {
            if (RepositoriesCombobox.SelectedItem != null)
            {
                Repository r = (Repository)RepositoriesCombobox.SelectedItem;
                RepositoryNameEditTextBox.Text = r.RepositoryName;
                RepositoryAddressEditTextBox.Text = r.RepositoryAddress;
            }
        }

        private void Export()
        {
            CommonDialog dialog = new CommonDialog();
            dialog.InitialDirectory = People.ApplicationFolderPath;
            dialog.Filter.Add(new FilterEntry(Properties.Resources.htmlFiles, Properties.Resources.htmlExtension));
            dialog.Title = Properties.Resources.Export;
            dialog.DefaultExtension = Properties.Resources.DefaulthtmlExtension;
            dialog.ShowSave();

            if (!string.IsNullOrEmpty(dialog.FileName))
            {
                RepositoriesExport repostories = new RepositoriesExport();
                repostories.ExportRepositories(dialog.FileName, Path.GetFileName(App.FamilyCollection.FullyQualifiedFilename), App.Repositories);
            }

            if (File.Exists(dialog.FileName))
            {
                MessageBoxResult result = MessageBox.Show(Properties.Resources.SourcesExportMessage,
                Properties.Resources.ExportResult, MessageBoxButton.YesNo, MessageBoxImage.Question);

                try
                {
                    if (result == MessageBoxResult.Yes)
                        System.Diagnostics.Process.Start(dialog.FileName);
                }
                catch { }
            }
        }

        private void Clear()
        {
            RepositoriesCombobox.SelectedIndex = -1;
            RepositoryNameEditTextBox.Text=string.Empty;
            RepositoryAddressEditTextBox.Text=string.Empty;
        }

        private void Add()
        {
            int y = 0;

            string oldRepositoryIDs = string.Empty;

            foreach (Repository s in App.Repositories)
            {
                oldRepositoryIDs += s.Id + "E";
            }

            do
            {
                y++;
            }
            while (oldRepositoryIDs.Contains("R" + y.ToString() + "E"));

            string repositoryID = "R" + y.ToString();

            Repository newRepository = new Repository(repositoryID, "", "");
            App.Repositories.Add(newRepository);
            App.Repositories.OnContentChanged();
        }

        private void Save()
        {
            if (RepositoriesCombobox.Items != null && RepositoriesCombobox.Items.Count > 0 && RepositoriesCombobox.SelectedItem != null)
            {
                Repository r = (Repository)RepositoriesCombobox.SelectedItem;
                r.RepositoryName = RepositoryNameEditTextBox.Text;
                r.RepositoryAddress = RepositoryAddressEditTextBox.Text;
                r.OnPropertyChanged("RepositoryNameAndId");
            }
        }

        private void Delete()
        {
            if (RepositoriesCombobox.Items != null && RepositoriesCombobox.Items.Count > 0 && RepositoriesCombobox.SelectedItem != null)
            {
                Repository repositoryToRemove = (Repository)RepositoriesCombobox.SelectedItem;

                bool deletable = true;

                foreach (Source s in App.Sources)
                {
                    if (deletable)
                    {
                        if (s.SourceRepository == repositoryToRemove.Id)
                            deletable = false;
                    }
                }

                if (deletable)
                {
                    MessageBoxResult result = MessageBox.Show(Properties.Resources.ConfirmDeleteRepository, Properties.Resources.Repository, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        App.Repositories.Remove(repositoryToRemove);
                        App.Repositories.OnContentChanged();
                        Clear();
                    }

                }
                else
                    MessageBox.Show(string.Format(Properties.Resources.UnableDeleteRepository, repositoryToRemove.Id),
                                    Properties.Resources.Repository, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

    }
}