﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Threading;
using System.Windows;

namespace Simple_File_Sender
{
    public class Pinger
    {
        EventWaitHandle pingingHandle = new AutoResetEvent(false);
        System.Timers.Timer timer = new System.Timers.Timer();
        List<SocketAsyncEventArgs> list = new List<SocketAsyncEventArgs>();
        IPAddress[] IPs;
        List<NameIPPair> contacts = new List<NameIPPair>();

        public Pinger()
        {
            timer.Elapsed += timer_Elapsed;
            IPs = Dns.GetHostAddresses(Dns.GetHostName());
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            pingingHandle.Set();
            foreach (var s in list)
            {
                ((Socket)s.UserToken).Dispose();     // disposing all sockets that's pending or connected.
            }
        }

        /// <summary>
        /// Scans all local network adapters and returns active clients
        /// </summary>
        /// <param name="timeout">Timeout in miliseconds</param>
        /// <returns>List of all found active clients</returns>
        public Task<List<NameIPPair>> GetOnlineContacts(double timeout)
        {
            timer.Interval = timeout;
            Task<List<NameIPPair>> task = new Task<List<NameIPPair>>(new Func<List<NameIPPair>>(getOnlineContacts));
            task.Start();
            return task;
        }

        private List<NameIPPair> getOnlineContacts()
        {
            pingingHandle.Reset();
            timer.Start();
            foreach (IPAddress ip in IPs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    for (int i = 0; i < 255; i++)
                    {
                        string[] whoa = ip.ToString().Split('.');
                        whoa[3] = i.ToString();
                        string gate = String.Join(".", whoa);
                        ConnectTo(gate);
                    }
                }
            }
            pingingHandle.WaitOne();
            return contacts;
        }

        private void ConnectTo(string ip)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), 6967);
            e.UserToken = s;
            e.Completed += new EventHandler<SocketAsyncEventArgs>(e_Completed);
            list.Add(e);      // Add to a list so we dispose all the sockets when the timer ticks.
            s.ConnectAsync(e);
        }

        private void e_Completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.ConnectSocket != null && e.ConnectSocket.Connected)
            {
                try
                {
                    byte[] nameBuffer = new byte[sizeof(char) * 128];
                    e.ConnectSocket.Receive(nameBuffer);
                    string name = Helpers.GetString(nameBuffer).Replace("\0", String.Empty);
                    if (name != StaticPenises.BannedRefuseName)
                        contacts.Add(new NameIPPair(name, ((IPEndPoint)e.RemoteEndPoint).Address));
                }
                catch(IOException exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }
        }
    }
}
