using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsyncServer
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTimeServer server = new DateTimeServer(10001);
            Console.WriteLine("Start server");
            server.Start();
            Console.ReadLine();
        }
    }
}
