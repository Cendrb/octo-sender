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
            FilesFromNonContacts mode = (FilesFromNonContacts)Enum.Parse(typeof(FilesFromNonContacts), Properties.Settings.Default.FilesFromNonContacts);
            switch(mode)
            {
                case FilesFromNonContacts.ask:
                    AskRadio.IsChecked = true;
                    break;
                case FilesFromNonContacts.accept:
                    AcceptRadio.IsChecked = true;
                    break;
                case FilesFromNonContacts.reject:
                    RejectRadio.IsChecked = true;
                    break;
            }
            AskBeforeReceivingButton.IsChecked = Properties.Settings.Default.AskBeforeReceivingFile;
            ShowBlockedContactsButton.IsChecked = Properties.Settings.Default.ShowBannedContactsInContacts;
            ShowLocalClientButton.IsChecked = Properties.Settings.Default.ShowLocalClientInContacts;
            BlindBannedContactsButton.IsChecked = Properties.Settings.Default.BlindBannedContacts;
            VerifyMD5Button.IsChecked = Properties.Settings.Default.VerifyMD5;

            ShowDialog();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FilesFromNonContacts mode;
            if (AskRadio.IsChecked.Value)
                mode = FilesFromNonContacts.ask;
            else if (AcceptRadio.IsChecked.Value)
                mode = FilesFromNonContacts.accept;
            else
                mode = FilesFromNonContacts.reject;

            Properties.Settings.Default.AskBeforeReceivingFile = AskBeforeReceivingButton.IsChecked.Value;
            Properties.Settings.Default.FilesFromNonContacts = mode.ToString();
            Properties.Settings.Default.ShowBannedContactsInContacts = ShowBlockedContactsButton.IsChecked.Value;
            Properties.Settings.Default.ShowLocalClientInContacts = ShowLocalClientButton.IsChecked.Value;
            Properties.Settings.Default.BlindBannedContacts = BlindBannedContactsButton.IsChecked.Value;
            Properties.Settings.Default.VerifyMD5 = VerifyMD5Button.IsChecked.Value;
            Properties.Settings.Default.Save();

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
