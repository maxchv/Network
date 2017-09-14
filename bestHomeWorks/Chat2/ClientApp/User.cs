using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientApp
{
    public class User : INotifyPropertyChanged
    {
        private string name = "";
        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        public Socket Socket { get; set; }
        public int Port { get; set; }
        public string Ip { get; set; }

        public User(string ip = "127.0.0.1", int port = 10000)
        {
            Port = port;
            Ip = ip;
            Connection();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Connection()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            try
            {
                Socket.Connect(new IPEndPoint(IPAddress.Parse(Ip), Port));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}

