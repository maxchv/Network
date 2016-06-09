using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BroadcastReceiver
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 10001);
            UdpClient receiver = new UdpClient(ep);
            while(true)
            {
                IPEndPoint senderEP = null;
                byte[] buff = receiver.Receive(ref senderEP);
                Console.WriteLine("Received from {0}: {1}", senderEP, Encoding.ASCII.GetString(buff));
            }
        }
    }
}
