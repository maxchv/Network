using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace TestUDP
{
    public partial class Form1 : Form
    {
        delegate void AddTextDelegate(String text);
        private class StateObject
        {
            public Socket workSocket = null;
            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];

        }
        StateObject state = new StateObject();
        Socket socket;
        IAsyncResult RcptRes, SendRes;

        EndPoint ClientEP = new IPEndPoint(IPAddress.Any, 100);

        public Form1()
        {
            InitializeComponent();
        }      
        void AddText(String text)
        {
            textBox1.Text+=text;
        }       
        private void button1_Click(object sender, EventArgs e)
        {
            if (socket != null)
                return;
            socket=new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            socket.Bind(new IPEndPoint(IPAddress.Parse("10.2.21.129"), 100));

            state.workSocket = socket;
            RcptRes = socket.BeginReceiveFrom(state.buffer,
                0,
                StateObject.BufferSize,
                SocketFlags.None,
                ref ClientEP, 
                new AsyncCallback(Receive_Completed), state);            
        }
        void Receive_Completed(IAsyncResult ia)
        {
            try
            {
                StateObject so = (StateObject)ia.AsyncState;
                Socket client = so.workSocket;
                if (socket == null)
                    return;
                int readed = client.EndReceiveFrom(RcptRes, ref ClientEP);

                String strClientIP = ((IPEndPoint)ClientEP).Address.ToString();
                String str = String.Format("\nПолучено от {0}\r\n{1}\r\n",
                    strClientIP, System.Text.Encoding.Unicode.GetString(so.buffer, 0, readed));

                textBox1.BeginInvoke(new AddTextDelegate(AddText), str);

                RcptRes = socket.BeginReceiveFrom(state.buffer,
                    0,
                    StateObject.BufferSize,
                    SocketFlags.None,
                    ref ClientEP,
                    new AsyncCallback(Receive_Completed),
                    state);
            }
            catch (SocketException ex)
            { 
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            socket.Shutdown(SocketShutdown.Receive);
            socket.Close();
            socket = null;
            textBox1.Text = "";
        }
        private void button3_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.IP);
            byte[] buffer=System.Text.Encoding.Unicode.GetBytes(textBox2.Text);
            SendRes = socket.BeginSendTo(buffer, 0, buffer.Count(), 
                SocketFlags.None, 
                (EndPoint)new IPEndPoint(IPAddress.Parse("10.2.21.255"), 100),
                new AsyncCallback(Send_Completed),
                socket);
        }
        void Send_Completed(IAsyncResult ia)
        {
            Socket socket = (Socket)ia.AsyncState;
            socket.EndSend(SendRes);
            socket.Shutdown(SocketShutdown.Send);
            socket.Close();
        }
    }
}
