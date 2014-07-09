using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Simple_File_Sender
{
    public class Sender
    {
        public event Action<SenderTask> FileSent = delegate { };

        List<int> usedPorts;

        private ItemCollection tasks;

        TcpClient portSocket;

        public int RunningTasks
        {
            get
            {
                int counter = 0;
                foreach (object o in tasks)
                {
                    SenderTask task = o as SenderTask;
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

        public string Name { get; set; }

        public Sender(List<int> ports, ItemCollection collection, string name)
        {
            usedPorts = ports;
            tasks = collection;
            this.Name = name;
        }

        public async void SendFile(Contact contact, string file)
        {
            if (await contact.Ping() > -1)
            {
                sendFile(contact, file);
            }
            else
            {
                MessageBox.Show("Selected contact seems to be offline (cannot be pinged)\nFile cannot be send", "Failed to ping", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void sendFile(Contact contact, string file)
        {
            int port = await GetFreePortWith(contact.IP);
            SenderTask task;
            task = new SenderTask(file, new IPEndPoint(contact.IP, port), contact, Name);
            task.Delete += task_Delete;
            task.Completed += task_Completed;
            usedPorts.Add(port);
            tasks.Dispatcher.Invoke(() => tasks.Add(task));
            task.Start();
        }

        public void StopAllTasks()
        {
            foreach (object o in tasks)
            {
                SenderTask task = o as SenderTask;
                task.Stop();
            }
        }

        private void task_Completed(SenderTask task)
        {
            tasks.Dispatcher.Invoke(() => FileSent(task));
            usedPorts.Remove(task.Target.Port);
        }

        private void task_Delete(SenderTask task)
        {
            usedPorts.Remove(task.Target.Port);
            tasks.Dispatcher.Invoke(() => tasks.Remove(task));
        }

        private Task<int> GetFreePortWith(IPAddress address)
        {
            Task<int> task = new Task<int>(() => getFreePortWith(address));
            task.Start();
            return task;
        }

        private int getFreePortWith(IPAddress address)
        {
            portSocket = new TcpClient();
            portSocket.Connect(address, 6969);
            int port = 6970;
            bool freeLocal = false;
            bool freeRemote = false;

            try
            {

                while (!freeLocal && !freeRemote)
                {
                    freeLocal = false;
                    freeRemote = false;

                    while (!freeLocal)
                    {
                        if (usedPorts.Contains(port))
                            port++;
                        else
                            freeLocal = true;
                    }
                    while (!freeRemote)
                    {
                        portSocket.Client.Send(BitConverter.GetBytes(port));

                        byte[] portBuffer = new byte[sizeof(int)];
                        portSocket.Client.Receive(portBuffer);
                        int receivedPort = BitConverter.ToInt32(portBuffer, 0);

                        if (receivedPort == port)
                            freeRemote = true;
                        else
                            port++;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                portSocket.Close();
            }

            return port;
        }
    }
}
