using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    /// Interaction logic for ContactInput.xaml
    /// </summary>
    public partial class ContactInput : Window
    {
        public IPAddress Address { get; private set; }
        public bool Success { get; private set; }

        public ContactInput()
        {
            InitializeComponent();
            Success = false;
        }

        private void AddButt_Click(object sender, RoutedEventArgs e)
        {
            IPAddress address;
            if(IPAddress.TryParse(IPText.Text, out address))
            {
                this.Close();
                Address = address;
                Success = true;
            }
            else
                MessageBox.Show("Invalid IP", "Error");
        }
    }
}
