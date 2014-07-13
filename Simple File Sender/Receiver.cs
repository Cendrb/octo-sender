using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Simple_File_Sender
{
    public class Receiver
    {
        public event Action<ReceiverTask> FileReceived = delegate { };

        List<int> usedPorts;

        public ItemCollection Tasks { get; private set; }
        public ItemCollection Contacts { get; private set; }

        public string Name { get; set; }

        public bool Running { get; private set; }

        public StringCollection BannedIPs { get; set; }

        public int RunningTasks
        {
            get
            {
                int counter = 0;
                foreach (object o in Tasks)
                {
                    ReceiverTask task = o as ReceiverTask;
                    if (task.Running)
                        counter++;
                }
                return counter;
            }
        }

        public int TotalTasks
        {
            get
            {
                return Tasks.Count;
            }
        }

        TcpListener namePingerListener = new TcpListener(IPAddress.Any, StaticPenises.NamePingPort);
        TcpListener pingerListener = new TcpListener(IPAddress.Any, StaticPenises.PingPort);
        TcpListener portCommListener = new TcpListener(IPAddress.Any, StaticPenises.MainPort);

        public Func<string, string> Path { get; set; }

        public Receiver(List<int> usedPorts, ItemCollection tasks, ItemCollection contacts, Func<string, string> path, string name)
        {
            BannedIPs = new StringCollection();
            Name = name;
            this.usedPorts = usedPorts;
            this.Tasks = tasks;
            Contacts = contacts;
            Path = path;

            // Add used ports
            usedPorts.Add(6969); // Main comm port (Receiver class)
            usedPorts.Add(6967); // Name ping port (Receiver and Pinger class)
            usedPorts.Add(6968); // Ping port (Receiver class)
        }

        public void Start()
        {
            if (!Running)
            {
                Running = true;

                Thread thread = new Thread(new ThreadStart(PortListenerStart));
                thread.SetApartmentState(ApartmentState.MTA);
                thread.Name = "Port listener (6969) - Receiver class";
                thread.Start();

                Thread pinger = new Thread(new ThreadStart(PingerStart));
                pinger.Name = "Ping listener (6968) - Receiver class";
                pinger.Start();

                Thread namePinger = new Thread(new ThreadStart(NamePingerStart));
                namePinger.Name = "Name listener (6967) - Receiver class";
                namePinger.Start();
            }
            else
                Console.WriteLine("Receiver already running");
        }

        public void Stop()
        {
            if (Running)
            {
                Running = false;

                namePingerListener.Stop();
                pingerListener.Stop();
                portCommListener.Stop();
            }
            else
                Console.WriteLine("Receiver not running");
        }

        public void StopAllTasks()
        {
            foreach (object o in Tasks)
            {
                ReceiverTask task = o as ReceiverTask;
                task.Stop();
            }
        }

        private void NamePingerStart()
        {
            try
            {
                namePingerListener.Start();
                try
                {
                    while (Running)
                    {
                        TcpClient client = namePingerListener.AcceptTcpClient();
                        if (BannedIPs.Contains((client.Client.RemoteEndPoint as IPEndPoint).Address.ToString()) && Properties.Settings.Default.BlindBannedContacts)
                            client.Client.Send(Helpers.GetBytes(StaticPenises.BannedRefuseName, sizeof(char) * 128));
                        else
                            client.Client.Send(Helpers.GetBytes(Name, sizeof(char) * 128));
                        client.Close();
                    }
                }
                catch (SocketException e)
                {
                    if (Running)
                    {
                        Console.WriteLine(e.Message);
                        Restart();
                    }
                }
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message + "\nProgram may not be able to receive files.", "Unable to bind to port " + StaticPenises.NamePingPort);
                Console.WriteLine(e.Message + " port " + StaticPenises.NamePingPort);
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void PingerStart()
        {
            try
            {
                pingerListener.Start();

                try
                {
                    while (Running)
                    {
                        TcpClient client = pingerListener.AcceptTcpClient();
                        client.Close();
                    }
                }
                catch (SocketException e)
                {
                    if (Running)
                    {
                        Console.WriteLine(e.Message);
                        Restart();
                    }
                }
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message + "\nProgram may not be able to receive files.", "Unable to bind to port " + StaticPenises.PingPort);
                Console.WriteLine(e.Message + " port " + StaticPenises.PingPort);
            }
        }

        private void PortListenerStart()
        {
            try
            {
                portCommListener.Start();

                try
                {
                    while (Running)
                    {
                        try
                        {
                            TcpClient client = portCommListener.AcceptTcpClient();

                            IPEndPoint remoteEndpoint = (client.Client.RemoteEndPoint as IPEndPoint);

                            // Abort receiving if banned
                                if (BannedIPs.Contains(remoteEndpoint.Address.ToString()))
                                {
                                    Console.WriteLine("Refusing connection because IP " + client.Client.RemoteEndPoint.ToString() + " is banned");
                                    client.Client.Send(BitConverter.GetBytes(-1));
                                    throw new IPBannedException(remoteEndpoint.Address);
                                }

                            int receivedPort = 1;
                            int finalPort = 0;

                            while (receivedPort != 0)
                            {
                                byte[] portBuffer = new byte[sizeof(int)];
                                client.Client.Receive(portBuffer);
                                receivedPort = BitConverter.ToInt32(portBuffer, 0);

                                if (receivedPort == 0)
                                    Tasks.Dispatcher.Invoke(() => StartNewTask(IPAddress.Any, finalPort));
                                finalPort = 0;

                                bool localFree = false;
                                while (!localFree)
                                {
                                    if (!usedPorts.Contains(receivedPort))
                                    {
                                        client.Client.Send(BitConverter.GetBytes(receivedPort));
                                        finalPort = receivedPort;
                                        localFree = true;
                                    }
                                    else
                                        receivedPort++;
                                }
                            }
                        }
                        catch (IPBannedException e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (Running)
                    {
                        Console.WriteLine(e.Message);
                        Restart();
                    }
                }
            }
            catch (SocketException e)
            {
                MessageBox.Show(e.Message + "\nProgram may not be able to receive files.", "Unable to bind to port " + StaticPenises.MainPort);
                Console.WriteLine(e.Message + " port " + StaticPenises.MainPort);
            }
        }

        private bool IsInContacts(IPAddress address)
        {
            foreach(object o in Contacts)
            {
                Contact c = o as Contact;
                if (c.IP == address)
                    return true;
            }
            return false;
        }

        private void StartNewTask(IPAddress endPoint, int receivedPort)
        {
            ReceiverTask task = new ReceiverTask(endPoint, receivedPort, Path);
            usedPorts.Add(receivedPort);
            Tasks.Dispatcher.Invoke(() => Tasks.Add(task));
            task.AskBeforeReceiving = Properties.Settings.Default.AskBeforeReceivingFile;
            task.VerifyMD5 = Properties.Settings.Default.VerifyMD5;
            task.Delete += task_Delete;
            task.Completed += task_Completed;
            task.IsInContacts = IsInContacts;
            task.SuccessfullyCompleted += task_SuccessfullyCompleted;
            task.Start();
        }

        private void task_SuccessfullyCompleted(ReceiverTask task)
        {
            // Call Main thread event
            Tasks.Dispatcher.Invoke(() => FileReceived(task));
        }

        private void task_Completed(ReceiverTask task)
        {
            // Remove blocked port
            usedPorts.Remove(task.Port);
        }

        private void task_Delete(ReceiverTask task)
        {
            // Remove task
            Tasks.Dispatcher.Invoke(() => Tasks.Remove(task));
            // Remove blocked port
            usedPorts.Remove(task.Port);
        }
    }
}
