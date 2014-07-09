using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Simple_File_Sender
{
    public class Receiver
    {
        public event Action<ReceiverTask> FileReceived = delegate { };

        List<int> usedPorts;

        public ItemCollection tasks { get; private set; }

        public string Name { get; set; }

        public bool Running { get; private set; }

        public int RunningTasks
        {
            get
            {
                int counter = 0;
                foreach (object o in tasks)
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
                return tasks.Count;
            }
        }

        TcpListener namePingerListener = new TcpListener(IPAddress.Any, StaticPenises.NamePingPort);
        TcpListener pingerListener = new TcpListener(IPAddress.Any, StaticPenises.PingPort);
        TcpListener portCommListener = new TcpListener(IPAddress.Any, StaticPenises.MainPort);

        public Func<string, string> Path { get; set; }

        public Receiver(List<int> usedPorts, ItemCollection collection, Func<string, string> path, string name)
        {
            Name = name;
            this.usedPorts = usedPorts;
            tasks = collection;
            Path = path;
        }

        public void Start()
        {
            if (!Running)
            {
                Running = true;

                Thread thread = new Thread(new ThreadStart(PortListenerStart));
                thread.SetApartmentState(ApartmentState.MTA);
                thread.Name = "Port listener";
                thread.Start();

                Thread pinger = new Thread(new ThreadStart(PingerStart));
                pinger.Name = "Ping Receiver";
                pinger.Start();

                Thread namePinger = new Thread(new ThreadStart(NamePingerStart));
                namePinger.Name = "Name Ping Receiver";
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
            foreach (object o in tasks)
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
                        TcpClient client = portCommListener.AcceptTcpClient();

                        int receivedPort = 1;
                        int finalPort = 0;

                        while (receivedPort != 0)
                        {
                            byte[] portBuffer = new byte[sizeof(int)];
                            client.Client.Receive(portBuffer);
                            receivedPort = BitConverter.ToInt32(portBuffer, 0);

                            if (receivedPort == 0)
                                tasks.Dispatcher.Invoke(() => StartNewTask(IPAddress.Any, finalPort));
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

        private void StartNewTask(IPAddress endPoint, int receivedPort)
        {
            ReceiverTask task = new ReceiverTask(endPoint, receivedPort, Path);
            tasks.Dispatcher.Invoke(() => tasks.Add(task));
            task.Delete += task_Delete;
            task.Completed += task_Completed;
            task.Start();
        }

        private void task_Completed(ReceiverTask task)
        {
            tasks.Dispatcher.Invoke(() => FileReceived(task));
            usedPorts.Remove(task.Port);
        }

        private void task_Delete(ReceiverTask task)
        {
            tasks.Dispatcher.Invoke(() => tasks.Remove(task));
            usedPorts.Remove(task.Port);
        }
    }
}
