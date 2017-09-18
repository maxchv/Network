using ClassLibraryChatRoom;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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

namespace ChatRoom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<UserChat> Users { get; set; }
        private int port;
        private Socket receiverSocket;
        private Socket senderSocket;
        private IPEndPoint localEndPoint;
        public IPEndPoint broadcastEndPoint;
        public IPEndPoint BroadcastEndPoint { get { return broadcastEndPoint; } set { broadcastEndPoint = value; NotifyPropertyChanged("BroadcastEndPoint"); } }
        public IPEndPoint EndPoint { get { return localEndPoint; } set { localEndPoint = value; NotifyPropertyChanged("EndPoint"); } }
        private UserChat all;
        private UserChat currentUser;
        public UserChat CurrentUser { get { return currentUser; } set { currentUser = value; NotifyPropertyChanged("CurrentUser"); } }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeReceiverConnection()
        {
            while (true)
            {
                IpPortAddress dlg = new IpPortAddress("Слушатель (Any)");
                if (receiverSocket != null)
                    receiverSocket.Close();
                receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                if ((bool)dlg.ShowDialog())
                {
                    try
                    {

                        port = Int32.Parse(dlg.Port);
                        switch (dlg.IP)
                        {
                            case "default":
                                EndPoint = new IPEndPoint(IPAddress.Loopback, port);
                                break;
                            case "any":
                                EndPoint = new IPEndPoint(IPAddress.Any, port);
                                break;
                            default:
                                EndPoint = new IPEndPoint(IPAddress.Parse(dlg.IP), port);
                                break;
                        }
                        receiverSocket.Bind(localEndPoint);
                        Task.Run(() =>
                        {
                            while (receiverSocket.IsBound)
                            {
                                byte[] buff = new byte[1024];
                                EndPoint var = new IPEndPoint(IPAddress.Any, 10001);
                                int len = receiverSocket.ReceiveFrom(buff, ref var);
                                GetMessage(Encoding.Unicode.GetString(buff, 0, len), var.ToString());
                            }
                        });
                        break;
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); };
                }
                else
                {
                    EndPoint = new IPEndPoint(IPAddress.Any, 10001);
                    break;
                }
            }
        }

        private void GetMessage(string message, string broadcastEndPoint)
        {
            try
            {
                var v = UserChat.TryParseMessage(message);
                var newUser = Users.Where(u => u.RemoteEndPoint == broadcastEndPoint).FirstOrDefault();
                if (v.UserName != CurrentUser.Name)
                {
                    if (newUser == null)
                    {

                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            newUser = new UserChat() { Name = v.UserName, RemoteEndPoint = broadcastEndPoint.ToString() };
                            Users.Add(newUser);
                            newUser.Messages.Add(v);
                        }));
                    }
                    else
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            newUser.Name = v.UserName;
                            newUser.Messages.Add(v);
                        }));
                    }
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        all.AddMessage(message);
                    }));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    all.Messages.Add(new MessageChat() { Message = message, Time = DateTime.Now });
                }));
            }

        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Users = new ObservableCollection<UserChat>();
            all = new UserChat() { Name = "Общий" };
            Users.Add(all);
            InitializeUserName();
            InitializeConnections();
            UserInfo.DataContext = this;
            UsersList.ItemsSource = Users;
            UsersList.SelectedIndex = 0;
            ;
        }

        private void InitializeConnections()
        {
            InitializeSenderConnection();
            InitializeReceiverConnection();
        }

        private void InitializeSenderConnection()
        {
            while (true)
            {
                IpPortAddress dlg = new IpPortAddress("Удаленный сервер", "10.2.124.255");
                if (senderSocket != null)
                    senderSocket.Close();
                senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                senderSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                if ((bool)dlg.ShowDialog())
                {
                    try
                    {
                        port = Int32.Parse(dlg.Port);
                        switch (dlg.IP)
                        {
                            case "default":
                                BroadcastEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                                break;
                            case "any":
                                BroadcastEndPoint = new IPEndPoint(IPAddress.Any, port);
                                break;
                            default:
                                BroadcastEndPoint = new IPEndPoint(IPAddress.Parse(dlg.IP), port);
                                break;
                        }
                        break;
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); };
                }
                else
                {
                    BroadcastEndPoint = new IPEndPoint(IPAddress.Any, 10001);
                    break;
                }
            }

        }

        private void InitializeUserName()
        {
            CurrentUser = new UserChat();
            Users.Add(CurrentUser);
            InitilizeName();
        }

        private void ButtonConnection_Click(object sender, RoutedEventArgs e)
        {
            InitializeConnections();
        }

        private void ButtonName_Click(object sender, RoutedEventArgs e)
        {
            InitilizeName();
        }

        private void InitilizeName()
        {
            NameWindow dlg = new NameWindow();
            if ((bool)dlg.ShowDialog())
            {
                if (broadcastEndPoint != null && senderSocket != null)
                {
                    SendMessage($"{dlg.Name}:{CurrentUser.Name} -> {dlg.Name};");
                }
                CurrentUser.Name = dlg.Name;
            }
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Control || e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                SendMessageFromTextBox();
            }
        }

        private void SendMessageFromTextBox()
        {
            if (!String.IsNullOrEmpty(TextBoxMessage.Text))
            {
                SendMessage($"{CurrentUser.Name}:{TextBoxMessage.Text};");
                TextBoxMessage.Text = "";
            }
        }

        private void SendMessage(string message)
        {
            senderSocket.SendTo(Encoding.Unicode.GetBytes(message), broadcastEndPoint);
            CurrentUser.AddMessage(message);
            all.AddMessage(message);
        }

        private void ButtonBroadcast_Click(object sender, RoutedEventArgs e)
        {
            InitializeSenderConnection();
        }
    }
}
