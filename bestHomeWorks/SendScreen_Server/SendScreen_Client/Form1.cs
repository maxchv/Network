using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SendScreen_Client
{
    public partial class Form1 : Form
    {
        private int interval = 1000 / 60;
        public Form1()
        {
            InitializeComponent();
            numericUpDown1.Value = interval;
            Run();
        }
        private async void Run()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            EndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 10_000);

            while (true)
            {
                Image image = new Bitmap(await GetScreen());
                MemoryStream memoryStream = new MemoryStream();
                image.Save(memoryStream, ImageFormat.Jpeg);
                byte[] send = memoryStream.ToArray();
                int len = 0;

                int sended = 0;
                while (sended != send.Length)
                {
                    if (sended < send.Length - 500)
                    {
                        len = await Task.Factory.FromAsync(s.BeginSendTo(send, sended, 500, SocketFlags.None, endPoint, null, null),
                                                           s.EndSendTo);                            
                        sended += 500;
                    }
                    else
                    {
                        len = await s.SendTo(send, 500, send.Length - sended, SocketFlags.None, endPoint);
                        sended += send.Length - sended;
                    }
                }
                await s.SendTo(send, 0, 0, SocketFlags.None, endPoint);
                //Thread.Sleep(interval);
                await Task.Delay(interval);
                memoryStream.Close();
            }

        }
        private Task<Bitmap> GetScreen()
        {
            return Task.Run(() =>
            {
                Graphics graph = null;

                var bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                graph = Graphics.FromImage(bmp);

                graph.CopyFromScreen(0, 0, 0, 0, bmp.Size);

                return bmp;
            });
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            interval = (int)numericUpDown1.Value;
        }
    }
}
