using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpClientDemo
{


    class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect(IPAddress.Loopback, 10001);
            Console.Write("Type your message: ");
            string msg = Console.ReadLine();
            Console.WriteLine("Send message {0}", msg);
            
            using (Stream stream = client.GetStream())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(msg);    
                                            
                        string answer = reader.ReadLine();
                        
                        Console.WriteLine("Server answered {0}", answer);
                    }
                }
            }
        }
    }
}
