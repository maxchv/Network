using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientChatBykova
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        EndPoint broadcastEP;
        EndPoint localEP;
        //EndPoint remoteEP;

        private Brush foregroundStatus;

        public Brush ForgStatus
        {
            get { return foregroundStatus; }
            set { foregroundStatus = value; NotifyPropertyChanged("ForgStatus"); }
        }

        private string msgStatus;

        public string MsgStatus
        {
            get { return msgStatus; }
            set { msgStatus = value; NotifyPropertyChanged("MsgStatus"); }
        }


        private UserChat currUser = new UserChat();
        public UserChat CurrUser { get { return currUser; } set { currUser = value; NotifyPropertyChanged("CurrUser"); } }
        Socket clientSocket;
        ObservableCollection<UserChat> listUsers = new ObservableCollection<UserChat>();

        private string strMessage;

        public string StrMessage
        {
            get { return strMessage; }
            set
            {
                strMessage = value;
                NotifyPropertyChanged("StrMessage");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            listMessages.ItemsSource = listUsers;
            DataContext = this;

            clientSocket = new Socket(AddressFamily.InterNetwork,
                                             SocketType.Dgram,
                                             ProtocolType.Udp);
            //clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, true);
            //IPAddress serverAddress = IPAddress.Parse(ip.ToString());//IPAddress.Parse("10.2.124.23");

            CurrUser.CurrPort = 10_001;
            tbPORT.Text = CurrUser.CurrPort.ToString();
            broadcastEP = new IPEndPoint(IPAddress.Parse("10.2.124.255"), CurrUser.CurrPort);
            localEP = new IPEndPoint(IPAddress.Any, CurrUser.CurrPort);
            //remoteEP = new IPEndPoint(IPAddress.Any, 0);
            clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            clientSocket.Bind(localEP);
            //clientSocket.Listen(1000);

            ConnectedToServer();


        }

        private void ConnectedToServer()
        {
            CurrUser.CurrPort = Convert.ToInt32(tbPORT.Text);
            MsgStatus = "Вы подключились к чату";
            ForgStatus = Brushes.Gray;
            Task.Run(() =>
            {
                while (true)
                {
                    byte[] buff = new byte[1024];
                    int len;

                    len = clientSocket.ReceiveFrom(buff, ref localEP);
                    string msg = Encoding.Unicode.GetString(buff, 0, len);

                    string[] mss = msg.Split(':', ';');
                    /*List<string> mss2 = new List<string>();
                    foreach(string m in mss2)
                    {
                        if(m != "")
                        {
                            mss2.Add(m);
                        }
                    }*/

                    UserChat newMessage = new UserChat(mss[0], mss[1]);

                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //if (currUser.UserId != mss[0])
                        {
                            listUsers.Add(newMessage);
                        }
                    }));
                    Thread.Sleep(100);
                }

            });



            Thread.Sleep(200);


        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            currUser.UserMessage = StrMessage;
            if (currUser.UserName.Trim() == "")
            {
                currUser.UserName = "User";
            }
            if (StrMessage.Trim() != "")
            {
                Task.Run(() =>
                {
                    byte[] buff = Encoding.Unicode.GetBytes($"{currUser.UserName}:{StrMessage};");

                    clientSocket.SendTo(buff, broadcastEP);
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //listUsers.Add(new UserChat(currUser.UserName, currUser.UserMessage, true));
                        StrMessage = "";
                    }));
                });

            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            clientSocket = new Socket(AddressFamily.InterNetwork,
                                             SocketType.Dgram,
                                             ProtocolType.Udp);
            CurrUser.CurrPort = Convert.ToInt32(tbPORT.Text);
            broadcastEP = new IPEndPoint(IPAddress.Broadcast, CurrUser.CurrPort);
            localEP = new IPEndPoint(IPAddress.Any, CurrUser.CurrPort);

            ConnectedToServer();
        }
    }
}
