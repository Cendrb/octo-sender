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
    /// Interaction logic for BasicInfoDialog.xaml
    /// </summary>
    public partial class BasicInfoDialog : Window
    {
        public bool Success { get; private set; }

        public BasicInfoDialog()
        {
            Success = false;
            InitializeComponent();
        }

        private void SaveButt_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameText.Text != String.Empty)
            {
                this.Close();
                Success = true;
            }
            else
                Success = true;
        }
    }
}
