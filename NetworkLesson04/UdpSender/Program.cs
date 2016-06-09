using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpSender
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpClient client = new UdpClient();
            IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, 10001);
            byte[] buff = Encoding.UTF8.GetBytes("Hi");
            client.Send(buff, buff.Length, ep);
        }
    }
}
