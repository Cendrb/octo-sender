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
    /// Interaction logic for CloseDialog.xaml
    /// </summary>
    public partial class CloseDialog : Window
    {
        private CloseDialogResult result;
        public CloseDialog()
        {
            InitializeComponent();
            result = CloseDialogResult.None;
        }

        public CloseDialogResult ShowCloseDialog()
        {
            ShowDialog();
            return result;
        }

        private void minimize_Click(object sender, RoutedEventArgs e)
        {
            result = CloseDialogResult.Minimize;
            Close();
        }

        private void exit_Click(object sender, RoutedEventArgs e)
        {
            result = CloseDialogResult.Exit;
            Close();
        }

        private void back_Click(object sender, RoutedEventArgs e)
        {
            result = CloseDialogResult.Back;
            Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (result == CloseDialogResult.None)
                result = CloseDialogResult.Back;
        }
    }

    public enum CloseDialogResult { Minimize, Exit, Back, None }
}
