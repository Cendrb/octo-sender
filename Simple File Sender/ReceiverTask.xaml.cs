using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = Running));
            }
        }

        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public Func<string, string> SavePath { get; private set; }

        public int PacketSize { get; set; }
        public string SenderName { get; private set; }
        public string ReceivedFileName { get; private set; }
        public string FinalFile { get; private set; }

        public bool Success { get; private set; }

        public bool AskBeforeReceiving { get; set; }
        public bool VerifyMD5 { get; set; }

        public Func<IPAddress, bool> IsInContacts;
        public event Action<ReceiverTask> Completed = delegate { };
        public event Action<ReceiverTask> SuccessfullyCompleted = delegate { };
        public event Action<ReceiverTask> Delete = delegate { };

        private TcpListener listener;

        public ReceiverTask(IPAddress address, int port, Func<string, string> savePath)
        {
            VerifyMD5 = false;
            InitializeComponent();
            AskBeforeReceiving = false;
            Address = address;
            Port = port;
            Success = false;
            SavePath = savePath;
            PacketSize = StaticPenises.PacketSize;
        }

        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(start));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void start()
        {
            Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = false));
            Dispatcher.BeginInvoke(new Action(() => DeleteButton.IsEnabled = false));

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

            SenderName = hostName;
            ReceivedFileName = name;

            Thread.CurrentThread.Name = String.Format("ReceiverTask Thread receiving {0} from {1}", name, hostName);

            Dispatcher.BeginInvoke(new Action(() => setBasicLabels(name, hostName, (IPEndPoint)client.Client.LocalEndPoint)));

            Dispatcher.BeginInvoke(new Action(() => StopButton.IsEnabled = true));

            NetworkStream stream = client.GetStream();

            try
            {
                // "Double ask" protection
                bool askBeforeReceiving = true;
                if (IsInContacts != null)
                {
                    IPEndPoint remoteEndpoint = client.Client.RemoteEndPoint as IPEndPoint;
                    // Abort receiving if not in contacts + settings
                    if (!IsInContacts(remoteEndpoint.Address))
                    {
                        // IP not in contacts
                        switch ((FilesFromNonContacts)Enum.Parse(typeof(FilesFromNonContacts), Properties.Settings.Default.FilesFromNonContacts))
                        {
                            case FilesFromNonContacts.reject:
                                throw new NotInContactsException(remoteEndpoint.Address);
                            case FilesFromNonContacts.ask:
                                MessageBoxResult result = MessageBox.Show(String.Format("User {0} ({1}) is not in your contacts.\nDo you want to receive {2} ({3} kb) from him?", SenderName, remoteEndpoint.Address, ReceivedFileName, size.ToString("n", StaticPenises.Format)), "Unknown sender!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                                if (result == MessageBoxResult.No)
                                    throw new NotInContactsException(remoteEndpoint.Address);
                                else
                                    // "Double ask" protection
                                    askBeforeReceiving = false;
                                break;
                        }
                    }
                }

                // Allow aborting
                if (AskBeforeReceiving && askBeforeReceiving)
                {
                    MessageBoxResult result = MessageBox.Show(String.Format("Do you want to receive {0} from {1}?", ReceivedFileName, SenderName), ReceivedFileName, MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.No)
                        throw new SendingRefusedException("Sending was interrupted by user");
                }

                string path = getSavePath(ReceivedFileName, SenderName);

                if (path == String.Empty)
                    throw new SendingRefusedException("Sending was interrupted by user");

                // Send confirm signal
                client.Client.Send(BitConverter.GetBytes(true));

                FinalFile = Path.Combine(path, ReceivedFileName);

                using (FileStream file = new FileStream(FinalFile, FileMode.Create, FileAccess.Write, FileShare.Write))
                {

                    Stopwatch totalWatch = Stopwatch.StartNew();
                    Stopwatch secondWatch = Stopwatch.StartNew();

                    status("Receiving data...");
                    int recBytes;
                    byte[] recData = new byte[PacketSize];
                    long totalRecBytes = 0;
                    long lastSecondBytes = 0;
                    while ((recBytes = stream.Read(recData, 0, recData.Length)) > 0)
                    {
                        totalRecBytes += recBytes;
                        lastSecondBytes += recBytes;

                        if (totalRecBytes > size)
                        {
                            recBytes -= (int)(totalRecBytes - size);
                            totalRecBytes = size;
                        }

                        // Stop receiving file to receive MD5
                        if (BitConverter.ToInt32(recData, 0) == -69)
                            break;

                        // Write to file
                        file.Write(recData, 0, recBytes);

                        // Update labels and bars
                        Dispatcher.BeginInvoke(new Action(() => updateProgress(totalRecBytes, size)));
                        if (secondWatch.ElapsedMilliseconds > 1000)
                        {
                            // Runned each second

                            // Allow stop
                            if (!Running)
                                throw new InterruptedByUserException();

                            secondWatch.Restart();
                            Dispatcher.Invoke(new Action(() => updateSpeedAndTime(totalWatch.Elapsed.TotalMilliseconds, totalRecBytes, size, lastSecondBytes)));
                            lastSecondBytes = 0;
                        }
                    }
                    totalWatch.Stop();
                    secondWatch.Stop();
                }

                // Send sender information if this task wants MD5 verification
                client.Client.Send(BitConverter.GetBytes(VerifyMD5));

                if (VerifyMD5)
                {
                    status("Receiving MD5 sum...");
                    byte[] md5 = new byte[16];
                    client.Client.Receive(md5);

                    status("Validating file using MD5 sum...");
                    using (FileStream file = File.OpenRead(FinalFile))
                    {
                        using (MD5 outMD5 = MD5.Create())
                        {
                            byte[] hash = outMD5.ComputeHash(file);
                            if (!Enumerable.SequenceEqual(md5, hash))
                                throw new FileFormatException("File is damaged and probably could not be opened");
                        }
                    }
                }
                successfullyCompleted();
            }
            catch (IOException e)
            {
                status("Failed to save received file");
                Console.WriteLine(e.Message);
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
            catch (SendingRefusedException e)
            {
                // decline receiving
                client.Client.Send(BitConverter.GetBytes(false));
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch (FileFormatException e)
            {
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            catch (NotInContactsException e)
            {
                // decline receiving
                client.Client.Send(BitConverter.GetBytes(false));
                status(e.Message);
                Console.WriteLine(e.Message);
            }
            finally
            {
                Completed(this);
                stream.Close();
                client.Close();
                Stop();
                Dispatcher.BeginInvoke(new Action(() => OpenButton.IsEnabled = true));
                Dispatcher.BeginInvoke(new Action(() => DeleteButton.IsEnabled = true));
            }
        }

        private string getSavePath(string filename, string sender)
        {
            return SavePath(String.Format("Choose where do you want to save {0} from {1}", filename, sender));
        }

        private void updateProgress(long sentBytes, long totalBytes)
        {
            // ProgressLabel
            ProgressLabel.Content = String.Format("{0} kbytes received of {1} kbytes total", (sentBytes / 1000).ToString("n", StaticPenises.Format), (totalBytes / 1000).ToString("n", StaticPenises.Format));

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

        private void successfullyCompleted()
        {
            SuccessfullyCompleted(this);
            status("Completed");
            Success = true;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs ea)
        {
            try
            {
                System.Diagnostics.Process.Start(FinalFile);
            }
            catch (FileNotFoundException e)
            {
                status("File was not found");
                Console.WriteLine(e.Message);
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
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
            Dispatcher.BeginInvoke(new Action(() => Status.Content = text));
        }

        public void Stop()
        {
            Running = false;
            listener.Stop();
        }
    }
}
