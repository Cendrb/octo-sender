using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows;
using System.Globalization;
using System.Security.Cryptography;

namespace VPN_Penis_Sender
{
    public class Receiver
    {
        public event Action<String> Error = delegate { };
        public event Action<String> ServerError = delegate { };
        public event Action<double> SetMaxValueOfStatusBar = delegate { };
        public event Action<double> SetValueOfStatusBar = delegate { };
        public event Action<string> Status = delegate { };
        public event Action SoundFileReceived = delegate { };
        public event Action SoundPendingFile = delegate { };

        TcpListener listener = null;
        public bool ServerRunning
        {
            get;
            private set;
        }
        public void StartServer(Func<string, string> path, IPEndPoint ip)
        {
            try
            {
                listener = new TcpListener(ip);
                Status(String.Format("Starting server at {0}:{1}...", ip.Address, ip.Port));
                listener.Start();
                Status(String.Format("Server running at {0}", ip.ToString()));
                Status("Ready to receive on " + ip.ToString());
                ServerRunning = true;
                Thread thread = new Thread(() => receive(path));
                thread.Name = "Receiver";
                thread.Priority = ThreadPriority.AboveNormal;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            catch (Exception e)
            {
                ServerError("Failed to start the server - " + e.Message);
                ServerRunning = false;
            }
        }
        public void StopServer()
        {
            ServerRunning = false;
            listener.Stop();
            Status("Server stopped");
            Status("Idle");
        }
        private void receive(Func<string, string> path)
        {
            while (ServerRunning)
            {
                try
                {
                    NumberFormatInfo info = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
                    info.NumberGroupSeparator = " ";
                    info.NumberDecimalDigits = 0;

                    Status("Ready to receive on " + listener.LocalEndpoint.ToString());
                    TcpClient client = listener.AcceptTcpClient();
                    Status(String.Format("Connected to {0}", client.Client.LocalEndPoint.ToString()));

                    byte[] nameBuffer = new byte[sizeof(char) * 128];
                    client.Client.Receive(nameBuffer);
                    string name = Helpers.GetString(nameBuffer).Replace("\0", String.Empty);
                    byte[] sizeBuffer = new byte[sizeof(long)];
                    client.Client.Receive(sizeBuffer);
                    long size = BitConverter.ToInt64(sizeBuffer, 0);
                    byte[] md5 = new byte[16];
                    client.Client.Receive(md5);

                    Status(String.Format("Receiving {0} with size of {1} bytes from {2}...", name, size, client.Client.LocalEndPoint.ToString()));

                    SetMaxValueOfStatusBar(size);

                    SoundPendingFile();
                    string filePath = path("Choose where do you want to save " + name);

                    NetworkStream stream = client.GetStream();
                    Status("Creating file...");
                    FileStream file = new FileStream(Path.Combine(filePath, name), FileMode.Create, FileAccess.Write, FileShare.Write);
                    Status("Receiving data...");

                    SetValueOfStatusBar(0);

                    try
                    {
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
                            file.Write(recData, 0, recBytes);
                            Status(String.Format("{0} bytes received of {1} bytes", totalRecBytes.ToString("n", info), size.ToString("n", info)));

                            SetValueOfStatusBar(totalRecBytes);

                            if (!ServerRunning)
                                throw new InvalidOperationException("Receiving interrupted");
                        }
                        file.Close();
                        file = File.OpenRead(Path.Combine(filePath, name));
                        using (MD5 outMD5 = MD5.Create())
                        {
                            byte[] hash = outMD5.ComputeHash(file);
                            if (!Enumerable.SequenceEqual(md5, hash))
                                throw new InvalidOperationException("Received file's md5 sum is not indentical with source. File is damaged and probably could not be opened.");
                        }
                        Status(name + " successfully received");
                        SoundFileReceived(); // Play sound
                    }
                    catch (Exception e)
                    {
                        Error(e.Message);
                    }
                    finally
                    {
                        file.Close();
                        stream.Close();
                        client.Close();
                    }
                }
                catch(SocketException)
                {

                }
            }
        }
    }
}
