using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace MulticastServer
{
    public partial class Form1 : Form
    {
        static string message = "Hello network!!!";
        static int Interval = 1000;
        static void multicastSend()
        {
            while (true)
            {
                Thread.Sleep(Interval);
                Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                soc.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
                IPAddress dest = IPAddress.Parse("224.5.5.5");
                soc.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(dest));
                IPEndPoint ipep = new IPEndPoint(dest, 4567);
                soc.Connect(ipep);
                soc.Send(Encoding.Default.GetBytes(message));
                soc.Close();
            }
        }
        Thread Sender = new Thread(new ThreadStart(multicastSend));
        public Form1()
        {
            InitializeComponent();
            Sender.IsBackground = true;
            Sender.Start();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            message = textBox1.Text;
        }
    }
}
