using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows;
using System.Threading;
using System.Media;

namespace VPN_Penis_Sender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Receiver receiver = null;
        Sender sender = null;
        bool serverStarted;
        public static bool stop = false;
        bool ServerStarted
        {
            get
            {
                return serverStarted;
            }
            set
            {
                serverStarted = value;
                startServerButton.IsEnabled = !serverStarted;
                stopServerButton.IsEnabled = serverStarted;
                useDefaultPathCheckBox.IsEnabled = !serverStarted;
                pathTextBox.IsEnabled = !serverStarted;
                pathChangeButton.IsEnabled = !serverStarted;
                ipAddressTextBox.IsEnabled = !serverStarted;
                portTextBox.IsEnabled = !serverStarted;
                if (serverStarted)
                {
                    startServerButton.Content = "Running...";
                    startReceiver();
                }
                else
                {
                    startServerButton.Content = "Start receiver";
                    if (receiver.ServerRunning)
                        receiver.StopServer();
                }
            }
        }
        bool soundsOn = true;

        bool sending;
        bool Sending
        {
            get
            {
                return sending;
            }
            set
            {
                sending = value;
                stop = !sending;
                stopButton.IsEnabled = sending;
                sendFileButton.IsEnabled = !sending;
                if (sending)
                    sendFileButton.Content = "Sending...";
                else
                    sendFileButton.Content = "Send file";
            }
        }
        SoundPlayer pending;
        SoundPlayer failed;
        SoundPlayer sentOrReceived;

        public MainWindow()
        {
            InitializeComponent();
            //wConsoleManager.Show();
  
            pending = new SoundPlayer("rec.cyp");
            failed = new SoundPlayer("fap.cyp");
            sentOrReceived = new SoundPlayer("suck.cyp");

            try
            {
                sentOrReceived.Play();
            }
            catch
            {
                soundsOn = false;
            }

            receiver = new Receiver();
            receiver.Error += receiveError;
            receiver.SetMaxValueOfStatusBar += statusBarSetMax;
            receiver.SetValueOfStatusBar += statusBarSet;
            receiver.Status += receiveStatus;
            receiver.Status += message;
            receiver.SoundPendingFile += receiver_PendingFile;
            receiver.SoundFileReceived += receiver_FileReceived;

            sender = new Sender();
            sender.Status += message;
            sender.Error += sendError;
            sender.Error += sendStatus;
            sender.SoundFileSent += fileSent;
            sender.SetMaxValueOfStatusBar += statusBarSetMax;
            sender.SetValueOfStatusBar += statusBarSet;
            sender.Error += message;
            sender.Status += sendStatus;
            sender.SoundFileSent += sender_FileSent;

            ServerStarted = false;
            stopButton.IsEnabled = false;
        }

        #region Sounds
        private void receiver_FileReceived()
        {
            if (soundsOn)
                sentOrReceived.Play();
        }
        private void sender_FileSent()
        {   
            if (soundsOn)
                sentOrReceived.Play();
        }
        private void receiver_PendingFile()
        {
            if (soundsOn)
                pending.Play();
        }
        #endregion

        private void fileSent()
        {
            this.Dispatcher.BeginInvoke(new Action(() => Sending = false));
        }

        private void startReceiver()
        {
            try
            {
                IPAddress address; // IP validation
                if (ipAddressTextBox.Text == "")
                    address = IPAddress.Any;
                else if (!IPAddress.TryParse(ipAddressTextBox.Text, out address))
                    throw new ArgumentException("Invalid IP");

                int port; // port validation
                if (!int.TryParse(portTextBox.Text, out port))
                    throw new ArgumentException("Invalid port");

                // path validation
                Func<String, String> getPath = null;

                if (useDefaultPathCheckBox.IsChecked.Value)
                {
                    if (!Directory.Exists(pathTextBox.Text))
                        throw new ArgumentException("Directory does not exist");
                    else
                    {
                        string path = pathTextBox.Text;
                        getPath = new Func<String, String>((dildo) => path);
                    }
                }
                else
                {
                    getPath = this.getDirectoryPath;
                }
                receiver.StartServer(getPath, new IPEndPoint(address, port));
            }
            catch (ArgumentException e)
            {
                error(e.Message);
                ServerStarted = false;
            }
        }

        #region StatusBar
        private void statusBarSetMax(double max)
        {
            this.Dispatcher.BeginInvoke(new Action<double>((m) => progressBar.Maximum = m), max);
        }
        private void statusBarSet(double value)
        {
            this.Dispatcher.BeginInvoke(new Action<double>((m) => progressBar.Value = m), value);
        }
        #endregion

        #region Status labels
        private void sendStatus(string status)
        {
            this.Dispatcher.BeginInvoke(new Action<string>((s) => senderStatus.Content = s), status);
        }
        private void receiveStatus(string status)
        {
            this.Dispatcher.BeginInvoke(new Action<string>((s) => receiverStatus.Content = s), status);
        }
        #endregion

        #region Info
        private void sendError(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() => Sending = false));
            error(message);
        }

        private void receiveError(string message)
        {
            error(message);
        }

        private void receiveServerError(string message)
        {
            this.Dispatcher.BeginInvoke(new Action(() => ServerStarted = false));
            error(message);
        }

        private void error(string message)
        {
            if (soundsOn)
                failed.Play();
            Console.WriteLine(message);
            System.Windows.MessageBox.Show(message, "Pětečka!");
        }

        private void message(string message)
        {
            this.Dispatcher.BeginInvoke(new Action<string>((m) => statusTextBlock.Text = m), message);
            Console.WriteLine(message);
        }
        #endregion

        #region File dialogs
        private string getDirectoryPath(string message)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = message;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return dialog.SelectedPath;
            else
                return String.Empty;
        }
        private string getFilePath(string message)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Title = message;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return dialog.FileName;
            else
                return String.Empty;
        }
        #endregion

        #region Buttons
        private void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            ServerStarted = true;
        }

        private void stopServerButton_Click(object sender, RoutedEventArgs e)
        {
            ServerStarted = false;
        }

        private void pathChangeButton_Click(object sender, RoutedEventArgs e)
        {
            string psim;
            if ((psim = getDirectoryPath("Choose target directory")) != String.Empty)
            {
                pathTextBox.Text = psim;
                useDefaultPathCheckBox.IsChecked = true;
            }
        }

        private void sendFileButton_Click(object senderpenis, RoutedEventArgs args)
        {
            try
            {
                Sending = true;

                IPAddress address; // IP validation
                if (sendIpAddressTextBox.Text == "")
                    address = IPAddress.Any;
                else if (!IPAddress.TryParse(sendIpAddressTextBox.Text, out address))
                    throw new ArgumentException("Invalid IP");

                int port; // port validation
                if (!int.TryParse(sendPortTextBox.Text, out port))
                    throw new ArgumentException("Invalid port");

                // path validation
                string path = getFilePath("Choose file to send");

                FileInfo file = null;
                if (path == String.Empty)
                    Sending = false;
                else
                    file = new FileInfo(path);

                if (Sending)
                {
                    Thread thread = new Thread(() => sender.Send(file, new IPEndPoint(address, port)));
                    thread.Name = "Sender";
                    thread.Priority = ThreadPriority.AboveNormal;
                    thread.Start();
                }
            }
            catch (Exception e)
            {
                sendError(e.Message);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            // Stop sending
            stopButton.IsEnabled = false;
            sendFileButton.IsEnabled = true;
            stop = true;
        }
        #endregion

        #region Window handlers
        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ServerStarted = false;
            Sending = false;
            stop = true;
        }
        #endregion
    }
}
