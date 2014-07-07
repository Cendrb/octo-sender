using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Simple_File_Sender
{
    public class Receiver
    {
        List<int> usedPorts;

        public ItemCollection tasks { get; private set; }

        public static string Path { get; set; }

        public Receiver(List<int> usedPorts, ItemCollection collection, string path)
        {
            this.usedPorts = usedPorts;
            tasks = collection;
            Path = path;
        }

        public void Start()
        {
            Thread thread = new Thread(new ThreadStart(PortListener));
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Name = "Port listener";
            thread.Start();

            Thread pinger = new Thread(new ThreadStart(PingerStart));
            pinger.Name = "Pinger";
            pinger.Start();
        }

        private void PingerStart()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 6968);
            listener.Start();
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                byte[] nameBuffer = new byte[sizeof(char) * 128];
                client.Client.Receive(nameBuffer);
                Console.WriteLine(Helpers.GetString(nameBuffer));
                client.Client.Send(Helpers.GetBytes("vagina"));
            }
        }

        private void PortListener()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 6969);

            listener.Start();

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();

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

        private void StartNewTask(IPAddress endPoint, int receivedPort)
        {
            ReceiverTask task = new ReceiverTask(endPoint, receivedPort);
            tasks.Dispatcher.Invoke(() => tasks.Add(task));
            task.Delete += task_Delete;
            task.Completed += task_Completed;
            task.Start();
        }

        private void task_Completed(ReceiverTask task)
        {
            tasks.Dispatcher.Invoke(() => usedPorts.Remove(task.Port));
        }

        private void task_Delete(ReceiverTask task)
        {
            tasks.Dispatcher.Invoke(() => tasks.Remove(task));
        }
    }
}
