using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpServer
{
    public class MyUdpServer
    {
        EndPoint epRemote = new IPEndPoint(IPAddress.Any, 0);
        byte[] buff = new byte[1024];

        public MyUdpServer()
        {
        }
        
        public void Up(int port = 10001)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
            
            Console.WriteLine("Listen for new connection");
            socket.BeginReceiveFrom(buff, 0, buff.Length, 
                                    SocketFlags.None, ref epRemote, 
                                    ReseiveCallback, socket);            
        }

        private void ReseiveCallback(IAsyncResult ar)
        {
            Socket socket = ar.AsyncState as Socket;
            int count = socket.EndReceiveFrom(ar, ref epRemote);
            Console.WriteLine("Recieved data from {0}", epRemote);
            Console.WriteLine("Data: {0}", Encoding.UTF8.GetString(buff, 0, count));

            socket.BeginReceiveFrom(buff, 0, buff.Length,
                                    SocketFlags.None, ref epRemote,
                                    ReseiveCallback, socket);
        }
    }
}
