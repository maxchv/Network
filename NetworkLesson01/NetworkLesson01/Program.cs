using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLesson01
{
    class Program
    {
        static void Main(string[] args)
        {
            string host = "itstep.dp.ua";
            IPAddress ip = Dns.GetHostAddresses(host).First();
            //Console.WriteLine(ip);
            IPEndPoint ep = new IPEndPoint(ip, 80);

            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                s.Connect(ep);
                if(s.Connected)
                {
                    Console.WriteLine("Connected");
                    // запрос на получение страницы 
                    string msg = string.Format("GET / HTTP/1.1\r\nHost: {0}\r\n\r\n", host);
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
