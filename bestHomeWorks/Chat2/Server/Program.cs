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
        static List<Socket> clients = null;
        static void Main(string[] args)
        {
            clients = new List<Socket>();
            Socket serverSocket = serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, 10000);
            serverSocket.Bind(iPEndPoint);
            serverSocket.Listen(5);
            Task task = Task.Run(() =>
            {
                while (true)
                {
                    Console.WriteLine("Wait connection...");
                    Socket clientSocket = serverSocket.Accept();
                    string notify = $"{clientSocket.RemoteEndPoint.ToString()} Подключен";
                    Console.WriteLine(notify);
                    foreach (var item in clients)
                    {
                        if (item != clientSocket)
                        {
                            item.Send(Encoding.UTF8.GetBytes(notify));
                        }
                    }
                    lock (clientSocket)
                    {
                        clients.Add(clientSocket);
                    }
                    ConversationStart(clientSocket);
                }
            });
            task.Wait();

            //Console.ReadLine();
            //serverSocket.Close();
        }

        private static Task ConversationStart(Socket clientSocket)
        {
            return Task.Run(() =>
            {
                byte[] buff = new byte[255];
                string msg = "";
                while (true)
                {
                    msg = "";
                    try
                    {
                        int len = clientSocket.Receive(buff);
                        msg = Encoding.UTF8.GetString(buff, 0, len);
                        if (string.IsNullOrWhiteSpace(msg)) throw new SocketException();
                        Console.WriteLine(msg);
                        foreach (var item in clients)
                        {
                            if (item != clientSocket)
                            {
                                item.Send(Encoding.UTF8.GetBytes(msg));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($"Клиент отключился {clientSocket.RemoteEndPoint}");
                        lock (clientSocket)
                        {
                            clients.Remove(clientSocket);
                        }
                        break;
                    }
                    if (!clientSocket.Connected)
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString()}] Клиент отключился {clientSocket.RemoteEndPoint}");
                        lock (clientSocket)
                        {
                            clients.Remove(clientSocket);
                        }
                        break;
                    }
                }
            });
        }
    }
}