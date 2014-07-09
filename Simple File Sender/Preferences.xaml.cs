using System;
using System.Collections.Generic;
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
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class Preferences : Window
    {
        public Preferences()
        {
            InitializeComponent();
        }

        public void ShowDialogLoadAndSave()
        {
            ShowBlockedContactsButton.IsChecked = Properties.Settings.Default.ShowBannedContactsInContacts;
            ShowLocalClientButton.IsChecked = Properties.Settings.Default.ShowLocalClientInContacts;
            BlindBannedContactsButton.IsChecked = Properties.Settings.Default.BlindBannedContacts;

            ShowDialog();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ShowBannedContactsInContacts = ShowBlockedContactsButton.IsChecked.Value;
            Properties.Settings.Default.ShowLocalClientInContacts = ShowLocalClientButton.IsChecked.Value;
            Properties.Settings.Default.BlindBannedContacts = BlindBannedContactsButton.IsChecked.Value;
            Properties.Settings.Default.Save();

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
