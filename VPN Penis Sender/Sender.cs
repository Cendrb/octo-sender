using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;

namespace VPN_Penis_Sender
{
    public class Sender
    {
        private TcpClient client;
        public event Action<String> Error = delegate { };
        public event Action<string> Status = delegate { };
        public event Action SoundFileSent = delegate { };
        public event Action<double> SetMaxValueOfStatusBar = delegate { };
        public event Action<double> SetValueOfStatusBar = delegate { };

        public bool Send(FileInfo file, IPEndPoint ip)
        {
            NumberFormatInfo info = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            info.NumberGroupSeparator = " ";
            info.NumberDecimalDigits = 0;

            client = new TcpClient();
            Status(String.Format("Reading {0}...", file.FullName));
            if(!file.Exists)
                Error(String.Format("File {0} does not exist!", file.Name));
            if (connect(ip))
            {
                try
                {
                    Status(String.Format("Sending {0} ({1} bytes) to {2}...", file.Name, file.Length, ip.ToString()));
                    client.Client.Send(Helpers.GetBytes(file.Name, sizeof(char) * 128));
                    client.Client.Send(BitConverter.GetBytes(file.Length));

                    FileStream stream = File.OpenRead(file.FullName);
                    using (MD5 md5 = MD5.Create())
                    {
                        client.Client.Send(md5.ComputeHash(stream));
                    }
                    stream = File.OpenRead(file.FullName);

                    long size = file.Length;
                    int readBytes;
                    byte[] readData = new byte[65536];
                    int totalReadBytes = 0;
                    SetMaxValueOfStatusBar(size);
                    while ((readBytes = stream.Read(readData, 0, readData.Length)) > 0)
                    {
                        client.Client.Send(readData);
                        totalReadBytes += readBytes;
                        Status(String.Format("{0} bytes sent of {1} bytes", totalReadBytes.ToString("n", info), size.ToString("n", info)));

                        SetValueOfStatusBar(totalReadBytes);

                        if (MainWindow.stop)
                            throw new InvalidOperationException("Sending interrupted by user");
                    }
                    Status(String.Format("{0} successfully sent", file.Name));
                    SoundFileSent();
                    return true;
                }
                catch (Exception e)
                {
                    Error(String.Format("Failed to send {0} to {1}:{2} - {3}", file.Name, ip.Address, ip.Port.ToString(), e.Message));
                    return false;
                }
                finally
                {
                    client.Close();
                }
            }
            return false;
        }
        private bool connect(IPEndPoint ip)
        {
            Status(String.Format("Connecting to server ({0})...", ip.ToString()));
            try
            {
                client.Connect(ip);
                return true;
            }
            catch (Exception e)
            {
                Error("Failed to connect to the server - " + e.Message);
                return false;
            }
        }
    }
}
