using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BroadcastSender
{
    static class ExtendedUdpClient
    {
        public static int Send(this UdpClient udp, string data, IPEndPoint ep)
        {
            byte[] buff = Encoding.ASCII.GetBytes(data);
            return udp.Send(buff, buff.Length, ep);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            UdpClient sender = new UdpClient();
            sender.EnableBroadcast = true;
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, 10001);
            while(true)
            {
                string time = DateTime.Now.ToLongTimeString();
                Console.WriteLine(time);
                sender.Send(time, ep);
                Thread.Sleep(1000);
            }
        }
    }
}
