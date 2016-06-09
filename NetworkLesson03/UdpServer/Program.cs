using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            #region example sync
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //socket.Bind(new IPEndPoint(IPAddress.Loopback, 10001));
            //EndPoint epRemote = new IPEndPoint(IPAddress.Any, 10001);
            //byte[] buff = new byte[1024];
            //Console.WriteLine("Listen for new connection");
            //int count = socket.ReceiveFrom(buff, ref epRemote);
            //Console.WriteLine("Recieved data from {0}", epRemote);
            //Console.WriteLine("Data: {0}", Encoding.UTF8.GetString(buff, 0, count));
            #endregion

            MyUdpServer server = new MyUdpServer();
            server.Up();
            Console.WriteLine("Start server");
            Console.ReadLine();
        }
    }
}
