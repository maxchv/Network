using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            //Console.WriteLine(ip);
            IPEndPoint ep = new IPEndPoint(ip, 10000);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                s.Connect(ep);
                if (s.Connected)
                {
                    Console.Write("Type message: ");
                    // запрос на получение страницы 
                    string msg = Console.ReadLine();
                    byte[] buff = Encoding.ASCII.GetBytes(msg);
                    s.Send(buff);

                    // получение ответа
                    int count = 0;
                    buff = new byte[1024];
                    do
                    {
                        count = s.Receive(buff);
                        Console.WriteLine(Encoding.UTF8.GetString(buff, 0, count));
                    } while (count > 0);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                
            }
        }
    }
}
