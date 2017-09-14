using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static List<Socket> clients = new List<Socket>();
        static Socket socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        static void Main(string[] args)
        {
            IPAddress address = IPAddress.Loopback;
            int port = 10000;
            socketServer.Bind(new IPEndPoint(address, port));
            socketServer.Listen(5);
            Console.WriteLine("Сервер начал работу...");
            while (true)
            {
                Socket client = socketServer.Accept();
                if (!clients.Contains(client))
                {
                    clients.Add(client);
                    //byte[] arr = ReceiveAll();
                    //string firstMessage = Encoding.UTF8.GetString(arr);
                    //if (!String.IsNullOrWhiteSpace(firstMessage))
                    //    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} {firstMessage}");
                    Task.Run(() =>
                    {
                        while (true)
                        {                            
                            try
                            {
                                byte[] buff = ReceiveAll(client);
                                string message = Encoding.UTF8.GetString(buff);
                                if (!String.IsNullOrWhiteSpace(message))
                                {
                                    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} {message}");
                                    SendMessageAllClient($"{message}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                                if (!client.Connected && clients.Contains(client))
                                    clients.Remove(client);
                                break;
                            }
                        }
                    });
                }
            }
            Console.ReadKey();
        }
        public static byte[] ReceiveAll(Socket socket)
        {
            var buffer = new List<byte>();

            while (socket.Available > 0)
            {
                var currByte = new Byte[1];
                var byteCounter = socket.Receive(currByte, currByte.Length, SocketFlags.None);

                if (byteCounter.Equals(1))
                {
                    buffer.Add(currByte[0]);
                }
            }

            return buffer.ToArray();
        }


        private static void SendMessageAllClient(string message)
        {
            if (socketServer != null)
            {
                try
                {
                    var listRemove = new List<Socket>();
                    foreach (var client in clients)
                    {
                        try
                        {
                            client.Send(Encoding.UTF8.GetBytes(message));
                        }
                        catch
                        {
                            listRemove.Add(client);
                        }
                    }
                    foreach (var client in listRemove)
                    {
                        clients.Remove(client);
                    }
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
