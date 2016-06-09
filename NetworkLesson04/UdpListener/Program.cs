using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpListener
{
    class Program
    {
        static void Main(string[] args)
        {
            UdpClient udp = new UdpClient(10001);
            IPEndPoint ep = null;           
            byte[] data = udp.Receive(ref ep);            
            Console.WriteLine(Encoding.UTF8.GetString(data));
            //Process.Start("notepad.exe");
        }
    }
}
