using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            string strMsg = "";
            do
            {
                Console.Write("Type your message: ");
                strMsg = Console.ReadLine();
                byte[] msg = Encoding.UTF8.GetBytes(strMsg);
                socket.SendTo(msg, new IPEndPoint(IPAddress.Loopback, 10001));
            } while (strMsg.ToLower() != "bay");
        }
    }
}
