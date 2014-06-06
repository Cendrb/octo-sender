using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using VPN_Penis_Sender;

namespace Kýp_Sajlink
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("192.168.1.4", 6969);
            client.Client.Send(Helpers.GetBytes("pierdole.mp3", sizeof(char) * 128));
        }
    }
}
