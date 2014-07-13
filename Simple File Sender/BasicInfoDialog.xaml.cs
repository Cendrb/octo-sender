using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for BasicInfoDialog.xaml
    /// </summary>
    public partial class BasicInfoDialog : Window
    {
        public bool Success { get; private set; }

        private string defaultSavePath;
        public string DefaultSavePath
        {
            get
            {
                return defaultSavePath;
            }
            set
            {
                defaultSavePath = value;
                PathTextBlock.Text = defaultSavePath;
            }
        }

        public BasicInfoDialog()
        {
            Success = false;
            InitializeComponent();
        }

        private void SaveButt_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameText.Text != String.Empty && (Directory.Exists(DefaultSavePath) || AskCheckbox.IsChecked.Value))
            {
                this.Close();
                Success = true;
            }
            else
            {
                if (UsernameText.Text == String.Empty)
                    MessageBox.Show("Username cannot be empty", "Invalid username", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!(Directory.Exists(DefaultSavePath) || AskCheckbox.IsChecked.Value))
                    MessageBox.Show("You must choose valid path or check \"Ask on every incoming file\"", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Error);
                Success = false;
            }
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.Description = "Choose default save folder for received files.";
            folderDialog.ShowDialog();
            DefaultSavePath = folderDialog.SelectedPath;
        }

        private void AskCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DefaultButton.IsEnabled = false;
            PathTextBlock.IsEnabled = false;
        }

        private void AskCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DefaultButton.IsEnabled = true;
            PathTextBlock.IsEnabled = true;
        }

        private void CancelButt_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
