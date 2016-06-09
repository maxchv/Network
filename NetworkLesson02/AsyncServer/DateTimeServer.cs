using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncServer
{
    class DateTimeServer: IDisposable
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        IPEndPoint ep = null;
        byte[] buff = new byte[1024];

        public DateTimeServer(int port)
        {
            ep = new IPEndPoint(IPAddress.Loopback, port);
        }

        public void Start()
        {
            socket.Bind(ep);
            Console.WriteLine("Binded {0}", ep);
            socket.Listen(10);
            socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);
        }

        private Task<Socket> AcceptAsync()
        {
            return Task<Socket>.Factory.FromAsync(socket.BeginAccept,
                                                  socket.EndAccept, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket s = ar.AsyncState as Socket;
            Socket client = s.EndAccept(ar);
            Console.WriteLine("Listen for {0}", client.RemoteEndPoint);
            
            client.BeginReceive(buff, 0, buff.Length, 
                                SocketFlags.None, 
                                new AsyncCallback(ReceiveCallback), 
                                client);

            socket.BeginAccept(new AsyncCallback(AcceptCallback), s);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Socket c = ar.AsyncState as Socket;
            int count = c.EndReceive(ar);
            string msg = Encoding.ASCII.GetString(buff, 0, count);

            Console.WriteLine("Message from client {0}: {1} ", c.RemoteEndPoint, msg);

            if(msg == "time")
            {
                // return time
            }
            else if(msg == "date")
            {
                // return date
            }
            else
            {
                // error
            }
        }

        public void Dispose()
        {
            if(socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }
    }
}
