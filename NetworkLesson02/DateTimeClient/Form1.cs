using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DateTimeClient
{
    public partial class Form1 : Form
    {
        Socket client = null;
        readonly string strConnect = "Connect";
        readonly string strDisconnect = "Disconnect";
        public Form1()
        { 
            InitializeComponent();
            btnConnect.Text = strConnect;
            FormClosing += Form1_FormClosing;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == strConnect && client == null)
            {
                 client = new Socket(AddressFamily.InterNetwork,
                                   SocketType.Stream,
                                   ProtocolType.IP);
                string strPort = tbPort.Text;
                try
                {
                    int port = Convert.ToInt32(strPort);
                    IPEndPoint ep = new IPEndPoint(IPAddress.Loopback, port);
                    client.Connect(ep);                    
                    btnConnect.Text = strDisconnect;
                    tbPort.Enabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    toolStripStatusLabel1.Text = ex.Message;
                }
            }
            else if (btnConnect.Text == strDisconnect)
            {
                Disconnect();
                btnConnect.Text = strConnect;
                tbPort.Enabled = true;
                client = null;
            }
        }

        private void Disconnect()
        {
            if (client != null && client.Connected)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        

        private void btnGetTime_Click(object sender, EventArgs e)
        {
            // получить время и вывести в Label
        }

        private void btnGetDate_Click(object sender, EventArgs e)
        {
            // получить дату и вывести в Label
        }
    }
}
