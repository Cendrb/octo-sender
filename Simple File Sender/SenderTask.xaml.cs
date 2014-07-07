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

        public Contact TargetContact { get; private set; }
        public IPEndPoint Target { get; private set; }
        public FileInfo SourceFile { get; private set; }
        public TcpClient Client { get; private set; }
        public string SenderName { get; private set; }

        public SenderTask(string filePath, IPEndPoint target, Contact contact, string senderName)
        {
            InitializeComponent();
            TargetContact = contact;
            Running = false;
            SenderName = senderName;
            PacketSize = 65536;

            SourceFile = new FileInfo(filePath);
            if (!SourceFile.Exists)
                throw new FileNotFoundException("File does not exist");

            Target = target;

            Dispatcher.BeginInvoke(new Action(setBasicLabels));
        }

        public void Start()
        {
            Task.Factory.StartNew(start);
        }

        private void start()
        {
            Thread.CurrentThread.Name = "Sending " + SourceFile.Name;

            Running = true;

            // Connect
            status("Connecting to " + Target.ToString() + "...");

            try
            {
                Client = new TcpClient();

                status("Sending basic informations...");

                Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = false));
                Dispatcher.BeginInvoke(new Action(() => DeleteButton.IsEnabled = false));

                try
                {
                    Client.Connect(Target.Address, Target.Port);
                }
                catch(SocketException e)
                {
                    status("Failed to connect to client");
                    Console.WriteLine(e.Message);
                    Running = false;
                }

                Client.Client.Send(Helpers.GetBytes(SourceFile.Name, sizeof(char) * 128));
                Client.Client.Send(BitConverter.GetBytes(SourceFile.Length));
                Client.Client.Send(Helpers.GetBytes(SenderName, sizeof(char) * 128));

                Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = true));

                FileStream stream = File.OpenRead(SourceFile.FullName);
                
                status("Sending data...");

                Stopwatch totalWatch = Stopwatch.StartNew();
                Stopwatch secondWatch = Stopwatch.StartNew();

                long size = SourceFile.Length;
                int sentBytes;
                byte[] sentData = new byte[PacketSize];
                long totalSentBytes = 0;
                while ((sentBytes = stream.Read(sentData, 0, sentData.Length)) > 0)
                {
                    Client.Client.Send(sentData);
                    totalSentBytes += sentBytes;

                    if (!Running)
                        throw new InterruptedByUserException();

                    Dispatcher.Invoke(() => updateProgress(totalSentBytes, size));
                    if (secondWatch.ElapsedMilliseconds > 1000)
                    {
                        secondWatch.Restart();
                        Dispatcher.Invoke(() => updateSpeedAndTime(totalWatch.Elapsed, totalSentBytes, size));
                    }
                }
                totalWatch.Stop();
                secondWatch.Stop();

                Client.Client.Send(BitConverter.GetBytes(69));

                status("Generating MD5 sum...");

                stream = File.OpenRead(SourceFile.FullName);

                using (MD5 md5 = MD5.Create())
                {
                    Client.Client.Send(md5.ComputeHash(stream));
                }

                Completed(this);
                completed();
            }
            catch (SocketException e)
            {
                status("Failed to send file");
                Console.WriteLine(e.Message);
                Running = false;
            }
            catch (InterruptedByUserException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            finally
            {
                Stop();
                Client.Close();
                Dispatcher.BeginInvoke(new Action(() => DeleteButton.IsEnabled = true));
                if(Done)
                    Dispatcher.BeginInvoke(new Action(() => StartButton.IsEnabled = false));
            }
        }

        private void completed()
        {
            status("Completed");
            Done = true;
        }

        public void Stop()
        {
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
            Completed(this);
            Delete(this);
        }

        private void status(string text)
        {
            Dispatcher.BeginInvoke(new Action(() => Status.Content = text));
        }

        private void setBasicLabels()
        {
            FileName.Content = SourceFile.Name;
            setTargetLabel(Target, TargetContact.ContactName);
        }

        private void setTargetLabel(IPEndPoint target, string name)
        {
            TargetLabel.Content = String.Format("for {1} ({0})", target.ToString(), name);
        }

        private void updateProgress(long sentBytes, long totalBytes)
        {
            // ProgressLabel
            ProgressLabel.Content = String.Format("{0} kbytes sent of {1} kbytes total", (sentBytes / 1000).ToString("n", StaticPenises.Format), (totalBytes / 1000).ToString("n", StaticPenises.Format));

            // ProgressBar
            ProgressBar.Maximum = totalBytes;
            ProgressBar.Value = sentBytes;
        }

        private void updateSpeedAndTime(TimeSpan elapsedTime, long sentBytes, long totalBytes)
        {
            ElapsedTime.Content = elapsedTime.ToString(@"mm\:ss");

            double speed = (sentBytes / 1000) / elapsedTime.Seconds;
            SpeedValue.Content = speed + " kb/s";

            int remaining = (int)(((totalBytes - sentBytes) / 1000) / speed);
            TimeSpan remainingTime = new TimeSpan(0, 0, remaining);
            RemainingTime.Content = remainingTime.ToString(@"mm\:ss");
        }
    }
    public class InterruptedByUserException : Exception
    {
        public InterruptedByUserException()
            : base("Operation was interrupted by user")
        {

        }
    }
}
