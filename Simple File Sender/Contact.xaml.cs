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
        public event Action<Contact> Delete = delegate { };

        public string ContactName
        {
            get
            {
                return Pair.Name;
            }
            set
            {
                Pair.Name = value;
                PropertyChanged(this, new PropertyChangedEventArgs("FormattedIPAndName"));
            }
        }

        public NameIPPair Pair { get; private set; }
        private bool saved;
        public bool Saved
        {
            get
            {
                return saved;
            }
            set
            {
                saved = value;
                removeButton.IsEnabled = saved;
            }
        }
        public IPAddress IP
        {
            get
            {
                return Pair.IP;
            }
            set
            {
                Pair.IP = value;
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

        public Contact(NameIPPair pair)
        {
            this.Pair = pair;
            InitializeComponent();
            ContactName = pair.Name;
            IP = pair.IP;
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
            StatusLabel.Content = "Pinging...";
            PingLabel.Content = "Pinging...";
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

        public Task<long> Ping()
        {
            Task<long> task = new Task<long>(new Func<long>(ping));
            task.Start();
            return task;
        }

        private long ping()
        {
            TcpClient client = new TcpClient();
            IAsyncResult result = client.BeginConnect(IP, StaticPenises.PingPort, null, null);
            Stopwatch watch = Stopwatch.StartNew();
            result.AsyncWaitHandle.WaitOne(1000, true);
            watch.Stop();
            if (client.Connected)
            {
                client.Close();
                return watch.ElapsedMilliseconds;
            }
            else
            {
                client.Close();
                return -1;
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

        public static bool operator ==(Contact contact1, Contact contact2)
        {
            return contact1.Name == contact2.Name && contact1.IP == contact2.IP;
        }

        public static bool operator !=(Contact contact1, Contact contact2)
        {
            return !(contact1.Name == contact2.Name && contact1.IP == contact2.IP);
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            Delete(this);
        }
    }
}
