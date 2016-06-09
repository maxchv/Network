using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkLesson04
{
    class Program
    {
        static TcpListener listener;
        static void Main(string[] args)
        {
            listener = new TcpListener(IPAddress.Loopback, 10001);
            listener.Start();
            Task.Run(async () =>
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Start convesation with {0}", client.Client.RemoteEndPoint);
                await StartListen(client);
                
            });
            Console.WriteLine("Start listening");
            Console.ReadLine();
        }

        private async static Task StartListen(TcpClient client)
        {
            using (Stream stream = client.GetStream())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        string data = await reader.ReadLineAsync();
                        Console.WriteLine("Received {0}", data);
                        writer.AutoFlush = true;
                        await writer.WriteLineAsync("Hello there");
                    }
                }
            }
        }
    }
}
