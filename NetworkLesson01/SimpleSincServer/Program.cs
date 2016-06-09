using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleSincServer
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Loopback;
            //Console.WriteLine(ip);
            IPEndPoint ep = new IPEndPoint(ip, 10000);
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                s.Bind(ep);
                s.Listen(10);
                while(true)
                {
                    Console.WriteLine("Listen for client...");
                    Socket client = s.Accept();
                    Console.WriteLine(client.RemoteEndPoint);

                    byte[] buff = new byte[1024];
                    int count = client.Receive(buff);
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Received: " + Encoding.UTF8.GetString(buff, 0, count));
                    Console.ForegroundColor = ConsoleColor.White;
                    client.Send(buff, count, SocketFlags.None);
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
            }
            catch(Exception ex)
            {

            }
            finally
            {
                s.Shutdown(SocketShutdown.Both);
                s.Close();
            }
        }
    }
}
