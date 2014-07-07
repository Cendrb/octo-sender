using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for Contact.xaml
    /// </summary>
    public partial class Contact : UserControl, INotifyPropertyChanged
    {
        public event Action<Contact, string> SendFile = delegate { };
        private string contactName;
        public string ContactName
        {
            get
            {
                return contactName;
            }
            set
            {
                contactName = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FormattedIPAndName"));
            }
        }
        private IPAddress ip;
        public IPAddress IP
        {
            get
            {
                return ip;
            }
            set
            {
                ip = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FormattedIPAndName"));
            }
        }

        public string FormattedIPAndName
        {
            get
            {
                return String.Format("{0} ({1})", ContactName, IP.ToString());
            }
        }

        public Contact(string name, IPAddress ip)
        {
            InitializeComponent();
            ContactName = name;
            IP = ip;
            DataContext = this;
        }
        /*
        public void Ping()
        {
            Thread thread = new Thread(new ThreadStart(ping));
            thread.Start();
        }

        private void ping()
        {
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(IP, StaticPenises.PingPort);
                Stopwatch watch = Stopwatch.StartNew();
                client.Client.Send(Helpers.GetBytes("penis"));
                byte[] nameBuffer = new byte[sizeof(char) * 128];
                client.Client.Receive(nameBuffer);
                watch.Stop();
                Dispatcher.BeginInvoke(new Action<string>((s) => StatusLabel.Content = s), watch.ElapsedMilliseconds.ToString());
                Dispatcher.BeginInvoke(new Action<string>((s) => StatusLabel.Content = s), "Online");
            }
            catch (SocketException e)
            {
                Dispatcher.BeginInvoke(new Action<string>((s) => StatusLabel.Content = s), "Unreachable");
                Dispatcher.BeginInvoke(new Action<string>((s) => StatusLabel.Content = s), "Offline");
                Console.WriteLine(e.Message);
            }
            finally
            {
                client.Close();
            }
        }*/
        public async void PingAndView()
        {
            long ping = await Ping();
            if (ping > -1)
            {
                PingLabel.Content = ping.ToString() + " ms";
                StatusLabel.Content = "Online";
            }
            else
            {
                PingLabel.Content = "Unreachable";
                StatusLabel.Content = "Offline";
            }

        }

        public async Task<long> Ping()
        {
            TcpClient client = new TcpClient();
            try
            {
                await client.ConnectAsync(IP, StaticPenises.PingPort);
                Stopwatch watch = Stopwatch.StartNew();
                client.Client.Send(Helpers.GetBytes("penis"));
                byte[] nameBuffer = new byte[sizeof(char) * 128];
                client.Client.Receive(nameBuffer);
                Console.WriteLine(Helpers.GetString(nameBuffer));
                watch.Stop();
                return watch.ElapsedMilliseconds;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
            finally
            {
                client.Close();
            }


        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void sendFileButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = "Choose file to send";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && dialog.CheckFileExists)
            {
                SendFile(this, dialog.FileName);
            }
        }
    }
}
