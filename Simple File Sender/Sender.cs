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
        List<int> usedPorts;

        private ItemCollection tasks;

        TcpClient portSocket;

        public string Name { get; set; }

        public Sender(List<int> ports, ItemCollection collection, string name)
        {
            usedPorts = ports;
            tasks = collection;
            this.Name = name;
        }

        public void SendFile(Contact contact, string file)
        {
            //Thread thread = new Thread(() => sendFile(contact, file));
            //thread.SetApartmentState(ApartmentState.STA);
            //thread.Start();
            sendFile(contact, file);
        }

        private async void sendFile(Contact contact, string file)
        {
            if (await contact.Ping() > -1)
            {
                int port = GetFreePortWith(contact.IP);
                SenderTask task;
                task = new SenderTask(file, new IPEndPoint(contact.IP, port), contact, Name);
                task.Delete += task_Delete;
                task.Completed += task_Completed;
                usedPorts.Add(port);
                tasks.Dispatcher.Invoke(() => tasks.Add(task));
                task.Start();
            }
            else
                MessageBox.Show("NEsmysl špatně!");
        }

        private void task_Completed(SenderTask task)
        {
            usedPorts.Remove(task.Target.Port);
        }

        private void task_Delete(SenderTask task)
        {
            tasks.Dispatcher.Invoke(() => tasks.Remove(task));
        }

        private int GetFreePortWith(IPAddress address)
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
            catch (Exception)
            {

            }
            finally
            {
                portSocket.Close();
            }

            return port;
        }
    }
}
