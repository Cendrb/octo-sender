using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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
using System.Diagnostics;
using System.Globalization;

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for SenderTask.xaml
    /// </summary>
    public partial class SenderTask : UserControl
    {
        public event Action<SenderTask> Completed = delegate { };
        public event Action<SenderTask> SuccessfullyCompleted = delegate { };
        public event Action<SenderTask> Delete = delegate { };

        public int Timeout { get; set; }
        public int PacketSize { get; set; }

        private bool running;
        public bool Running
        {
            get
            {
                return running;
            }
            set
            {
                running = value;
                Dispatcher.BeginInvoke(new Action(() => StartButton.IsEnabled = !Running));
                Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = Running));
            }
        }
        public bool Done { get; private set; }

        public int Port { get; private set; }

        private Func<IPAddress, int> port;

        public Contact TargetContact { get; private set; }
        public FileInfo SourceFile { get; private set; }
        public TcpClient Client { get; private set; }
        public string SenderName { get; private set; }

        public SenderTask(string filePath, Func<IPAddress, int> port, Contact contact, string senderName)
        {
            InitializeComponent();
            TargetContact = contact;
            Running = false;
            SenderName = senderName;
            PacketSize = StaticPenises.PacketSize;
            this.port = port;

            SourceFile = new FileInfo(filePath);

            Dispatcher.BeginInvoke(new Action(() => updateProgress(0, SourceFile.Length)));

            Dispatcher.BeginInvoke(new Action(setBasicLabels));
        }

        public void Start()
        {
            Task.Factory.StartNew(start);
        }

        private async void start()
        {
            Thread.CurrentThread.Name = "Sending " + SourceFile.Name;

            Running = true;

            try
            {
                Client = new TcpClient();

                if (!SourceFile.Exists)
                    throw new FileNotFoundException("File does not exist");

                if (await TargetContact.Ping() < 0)
                    throw new UnaccesableRemoteClientException(TargetContact.IP);

                status("Connecting to " + TargetContact.IP + "...");

                // Gets free port
                Port = port(TargetContact.IP);

                if (Port == -1)
                    throw new IPBannedException(TargetContact);

                await Dispatcher.BeginInvoke(new Action(setBasicLabelsWithPort));

                // Connect
                status("Connecting to " + TargetContact.IP + ":" + Port.ToString() + "...");

                await Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = false));
                await Dispatcher.BeginInvoke(new Action(() => DeleteButton.IsEnabled = false));

                Client.Connect(TargetContact.IP, Port);


                status("Sending basic informations...");

                Client.Client.Send(Helpers.GetBytes(SourceFile.Name, sizeof(char) * 128));
                Client.Client.Send(BitConverter.GetBytes(SourceFile.Length));
                Client.Client.Send(Helpers.GetBytes(SenderName, sizeof(char) * 128));

                await Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = true));

                status("Waiting for opposite side...");

                byte[] continueBuffer = new byte[sizeof(bool)];
                Client.Client.Receive(continueBuffer);
                if (!BitConverter.ToBoolean(continueBuffer, 0))
                    throw new SendingRefusedException("Opposite side declined receiving this file");

                FileStream stream = File.OpenRead(SourceFile.FullName);

                status("Sending data...");

                Stopwatch totalWatch = Stopwatch.StartNew();
                Stopwatch secondWatch = Stopwatch.StartNew();

                long size = SourceFile.Length;
                int sentBytes;
                byte[] sentData = new byte[PacketSize];
                long totalSentBytes = 0;
                long lastSecondBytes = 0;
                while ((sentBytes = stream.Read(sentData, 0, sentData.Length)) > 0)
                {
                    // Send data
                    Client.Client.Send(sentData);
                    totalSentBytes += sentBytes;
                    lastSecondBytes += sentBytes;

                    Dispatcher.Invoke(() => updateProgress(totalSentBytes, size));
                    if (secondWatch.ElapsedMilliseconds > 1000)
                    {
                        // Runned every second

                        if (!Running)
                            throw new InterruptedByUserException();

                        secondWatch.Restart();
                        Dispatcher.Invoke(() => updateSpeedAndTime(totalWatch.Elapsed.TotalMilliseconds, totalSentBytes, size, lastSecondBytes));
                        lastSecondBytes = 0;
                    }
                }
                totalWatch.Stop();
                secondWatch.Stop();

                // Sends receiver information about end of file
                Client.Client.Send(BitConverter.GetBytes(69));

                // Gets information from receiver if it wants MD5 sum verification
                byte[] verifyMD5Buffer = new byte[sizeof(bool)];
                Client.Client.Receive(verifyMD5Buffer);
                if (BitConverter.ToBoolean(verifyMD5Buffer, 0))
                {
                    status("Generating MD5 sum...");

                    using (stream = File.OpenRead(SourceFile.FullName))
                    {
                        using (MD5 md5 = MD5.Create())
                        {
                            byte[] md5Hash = md5.ComputeHash(stream);
                            status("Sending MD5 sum...");
                            Client.Client.Send(md5Hash);
                        }
                    }
                }
                successfullyCompleted();
            }
            catch (SocketException e)
            {
                status("Sending was refused by opposite side");
                Console.WriteLine(e.Message);
            }
            catch (InterruptedByUserException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch (SendingRefusedException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch (FileNotFoundException e)
            {
                status("Source file does not exist");
                Console.WriteLine(e.Message);
            }
            catch (UnaccesableRemoteClientException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch (IPBannedException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            finally
            {
                Completed(this);
                Stop();
                Client.Close();
                Dispatcher.Invoke(new Action(() => DeleteButton.IsEnabled = true));
                if (Done)
                    Dispatcher.Invoke(new Action(() => StartButton.IsEnabled = false));
            }
        }

        private void successfullyCompleted()
        {
            SuccessfullyCompleted(this);
            status("Completed");
            Done = true;
        }

        public void Stop()
        {
            Dispatcher.Invoke(new Action(() => SpeedValue.Content = "0 kbps"));
            Dispatcher.Invoke(new Action(() => RemainingTime.Content = "00:00"));
            Dispatcher.Invoke(new Action(() => setBasicLabels()));
            Running = false;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Delete(this);
        }

        private void status(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => Status.Content = text));
        }

        private void setBasicLabels()
        {
            FileName.Content = SourceFile.Name;
            setTargetLabel(TargetContact.IP, TargetContact.ContactName);
        }

        private void setBasicLabelsWithPort()
        {
            FileName.Content = SourceFile.Name;
            setTargetLabel(TargetContact.IP, Port, TargetContact.ContactName);
        }

        private void setTargetLabel(IPAddress target, int port, string name)
        {
            TargetLabel.Content = String.Format("for {1} ({0}:{2})", target, name, port);
        }

        private void setTargetLabel(IPAddress target, string name)
        {
            TargetLabel.Content = String.Format("for {1} ({0})", target, name);
        }

        private void updateProgress(long sentBytes, long totalBytes)
        {
            // ProgressLabel
            ProgressLabel.Content = String.Format("{0} kbytes sent of {1} kbytes total", (sentBytes / 1000).ToString("n", StaticPenises.Format), (totalBytes / 1000).ToString("n", StaticPenises.Format));

            // ProgressBar
            ProgressBar.Maximum = totalBytes;
            ProgressBar.Value = sentBytes;
        }

        private void updateSpeedAndTime(double elapsedTime, long sentBytes, long totalBytes, long lastSecondBytes)
        {
            ElapsedTime.Content = TimeSpan.FromMilliseconds(elapsedTime).ToString(@"mm\:ss");

            double speed = (lastSecondBytes / 1000) / (1);
            SpeedValue.Content = speed + " kb/s";

            int remaining = (int)(((totalBytes - sentBytes) / 1000) / speed);
            RemainingTime.Content = TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
        }
    }
}
