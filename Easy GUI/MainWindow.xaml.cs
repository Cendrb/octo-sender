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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPN_Penis_Sender;

namespace Easy_GUI
{
    /// <summary>
    /// Interaction logic for Easy.xaml
    /// </summary>
    public partial class Easy : Window
    {
        private Receiver receiver;
        public Easy()
        {
            InitializeComponent();
            receiver = new Receiver();
            receiver.
        }
    }
}
