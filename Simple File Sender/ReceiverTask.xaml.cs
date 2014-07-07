using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
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

namespace Simple_File_Sender
{
    /// <summary>
    /// Interaction logic for ReceiverTask.xaml
    /// </summary>
    public partial class ReceiverTask : UserControl
    {
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
                Dispatcher.Invoke(new Action(() => StartButton.IsEnabled = !Running));
                Dispatcher.Invoke(new Action(() => StopButton.IsEnabled = Running));
            }
        }

        public IPAddress Address { get; private set; }
        public int Port { get; private set; }

        public bool Done { get; private set; }

        public event Action<ReceiverTask> Completed = delegate { };
        public event Action<ReceiverTask> Delete = delegate { };

        private TcpListener listener;

        public ReceiverTask(IPAddress address, int port)
        {
            InitializeComponent();
            Address = address;
            Port = port;
            Done = false;
        }

        public void Start()
        {
            Task.Factory.StartNew(start);
        }

        private void start()
        {
            Dispatcher.Invoke(new Action(() => StopButton.IsEnabled = false));
            Dispatcher.Invoke(new Action(() => DeleteButton.IsEnabled = false));

            Running = true;

            status("Waiting for client...");

            listener = new TcpListener(Address, Port);
            listener.Start();
            TcpClient client = listener.AcceptTcpClient();

            status("Receiving basic data...");

            byte[] nameBuffer = new byte[sizeof(char) * 128];
            client.Client.Receive(nameBuffer);
            string name = Helpers.GetString(nameBuffer).Replace("\0", String.Empty);
            byte[] sizeBuffer = new byte[sizeof(long)];
            client.Client.Receive(sizeBuffer);
            long size = BitConverter.ToInt64(sizeBuffer, 0);
            byte[] hostNameBuffer = new byte[sizeof(char) * 128];
            client.Client.Receive(hostNameBuffer);
            string hostName = Helpers.GetString(hostNameBuffer).Replace("\0", String.Empty);

            Dispatcher.Invoke(() => setBasicLabels(name, hostName, (IPEndPoint)client.Client.LocalEndPoint));

            NetworkStream stream = client.GetStream();
            FileStream file = new FileStream(Path.Combine(Receiver.Path, name), FileMode.Create, FileAccess.Write, FileShare.Write);

            Dispatcher.Invoke(new Action(() => StopButton.IsEnabled = true));

            try
            {
                Stopwatch totalWatch = Stopwatch.StartNew();
                Stopwatch secondWatch = Stopwatch.StartNew();

                status("Receiving data...");
                int recBytes;
                byte[] recData = new byte[65536];
                long totalRecBytes = 0;
                while ((recBytes = stream.Read(recData, 0, recData.Length)) > 0)
                {
                    totalRecBytes += recBytes;
                    if (totalRecBytes > size)
                    {
                        recBytes -= (int)(totalRecBytes - size);
                        totalRecBytes = size;
                    }

                    if (BitConverter.ToInt32(recData, 0) == 69)
                        break;

                    file.Write(recData, 0, recBytes);

                    if (!Running)
                        throw new InterruptedByUserException();

                    Dispatcher.Invoke(() => updateProgress(totalRecBytes, size));
                    if (secondWatch.ElapsedMilliseconds > 1000)
                    {
                        secondWatch.Restart();
                        Dispatcher.Invoke(() => updateSpeedAndTime(totalWatch.Elapsed, totalRecBytes, size));
                    }
                }
                totalWatch.Stop();
                secondWatch.Stop();
                file.Close();

                status("Receiving MD5 sum...");
                byte[] md5 = new byte[16];
                client.Client.Receive(md5);

                status("Validating file using MD5 sum...");
                file = File.OpenRead(Path.Combine(Receiver.Path, name));
                using (MD5 outMD5 = MD5.Create())
                {
                    byte[] hash = outMD5.ComputeHash(file);
                    if (!Enumerable.SequenceEqual(md5, hash))
                        throw new InvalidOperationException("File is damaged and probably could not be opened");
                }

                Completed(this);
                completed();
            }
            catch (SocketException e)
            {
                status("Failed to connect to client");
                Console.WriteLine(e.Message);
            }
            catch (InterruptedByUserException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch(InvalidOperationException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            finally
            {
                file.Close();
                stream.Close();
                client.Close();
                Stop();
                Dispatcher.Invoke(new Action(() => DeleteButton.IsEnabled = true));
                if(Done)
                    Dispatcher.Invoke(new Action(() => StartButton.IsEnabled = false)); 
            }
        }

        private void updateProgress(long sentBytes, long totalBytes)
        {
            // ProgressLabel
            ProgressLabel.Content = String.Format("{0} kbytes received of {1} kbytes total", (sentBytes / 1000).ToString("n", StaticPenises.Format), (totalBytes / 1000).ToString("n", StaticPenises.Format));

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

        private void completed()
        {
            status("Completed");
            Done = true;
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

        private void setBasicLabels(string fileName, string hostName, IPEndPoint address)
        {
            FileName.Content = fileName;
            setTargetLabel(address, hostName);
        }

        private void setTargetLabel(IPEndPoint target, string name)
        {
            TargetLabel.Content = String.Format("from {1} ({0})", target.ToString(), name);
        }

        private void status(string text)
        {
            Dispatcher.Invoke(new Action(() => Status.Content = text));
        }

        public void Stop()
        {
            Running = false;
            listener.Stop();
        }
    }
}
