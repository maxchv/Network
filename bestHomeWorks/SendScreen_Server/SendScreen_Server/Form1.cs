using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SendScreen_Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Run();
        }
        private async void Run()
        {
            await Task.Run(() =>
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                EndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 10_000);
                s.Bind(endPoint);
                while (true)
                {
                    byte[] buff = new byte[500];
                    int len = 0;
                    List<byte> msg = new List<byte>();
                    do
                    {
                        len = s.ReceiveFrom(buff, ref endPoint);
                        for (int i = 0; i < len; i++)
                        {
                            msg.Add(buff[i]);
                        }
                    } while (len != 0);
                    MemoryStream ms = new MemoryStream();
                    foreach (var b in msg)
                    {
                        ms.WriteByte(b);
                    }
                    Image img = Image.FromStream(ms);
                    pictureBox1.Image = img;
                }
            });
        }
    }
}
