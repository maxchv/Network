using ChatClient.Classes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        /* Протокол: <username>:<message>; */
        private int affixFiles = 0;
        private string affixPath = "";
        private string userName = "";
        public event PropertyChangedEventHandler PropertyChanged;

        Socket receiverSocket;
        EndPoint localEP;
        EndPoint remoteEP;
        Socket senderSocket;
        EndPoint broadcastEP;
        int PORT = 10001;

        public MainWindow()
        {
            InitializeComponent();
            UserName = "User";
            DataContext = this;
            Messages = new List<MessageInfo>();
            senderSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            broadcastEP = new IPEndPoint(IPAddress.Parse("10.2.124.255"), PORT);
            senderSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            Run();
        }
        public int AffixFiles
        {
            get { return affixFiles; }
            set
            {
                if (value != affixFiles)
                {
                    affixFiles = value;
                    OnPropertyChanged(nameof(AffixFiles));
                }
            }
        }
        public string UserName
        {
            get { return userName; }
            set
            {
                if (value != userName)
                {
                    userName = value;
                    OnPropertyChanged(nameof(UserName));
                }
            }
        }
        public List<MessageInfo> Messages { get; set; }
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private async void Run()
        {
            receiverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEP = new IPEndPoint(IPAddress.Any, PORT);
            receiverSocket.Bind(localEP);
            remoteEP = new IPEndPoint(IPAddress.Any, 0);
            await Task.Run(() =>
            {
                while (true)
                {
                    byte[] buff = new byte[500];
                    int len = receiverSocket.ReceiveFrom(buff, ref remoteEP);
                    string msg = Encoding.Unicode.GetString(buff, 0, len);
                    try
                    {
                        msg = msg.Insert(msg.IndexOf(':'), "]");
                        msg = msg.Insert(0, "[");
                        msg = msg.Replace(':', ' ').Replace(';', ' ');
                    }
                    catch(Exception)
                    {

                    }
                    if (msg.Contains(userName))
                    {
                        MessageInfo msgInfo = new MessageInfo
                        {
                            Text = msg,
                            Time = DateTime.Now.ToShortTimeString()
                        };
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageControl mc = new MessageControl()
                            {
                                MsgInfo = msgInfo
                            };
                            mc.ApplyStyles(ReceiverOrSender.Sender);
                            stackPanelMsgs.Children.Add(mc);
                        }));
                    }
                    else
                    {
                        MessageInfo msgInfo = new MessageInfo
                        {
                            Text = msg,
                            Time = DateTime.Now.ToShortTimeString()
                        };
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageControl mc = new MessageControl()
                            {
                                MsgInfo = msgInfo
                            };
                            mc.ApplyStyles(ReceiverOrSender.Receiver);
                            stackPanelMsgs.Children.Add(mc);
                        }));
                    }
                }
            });
        }
        private void Test()
        {
            Messages.AddRange(new MessageInfo[]
                        {
                new MessageInfo { Text = "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged.", Time = DateTime.Now.ToShortTimeString() },
                new MessageInfo { Text = "Some text", Time = DateTime.Now.ToShortTimeString() },
                new MessageInfo { Text = "Some text", Time = DateTime.Now.ToShortTimeString() }
                        });
            int counter = 0;
            foreach (var item in Messages)
            {
                if (counter % 2 == 0)
                {
                    MessageControl mc = new MessageControl()
                    {
                        MsgInfo = item
                    };
                    mc.ApplyStyles(ReceiverOrSender.Receiver);
                    stackPanelMsgs.Children.Add(mc);
                }
                else
                {
                    MessageControl mc = new MessageControl()
                    {
                        MsgInfo = item
                    };
                    mc.ApplyStyles(ReceiverOrSender.Sender);
                    stackPanelMsgs.Children.Add(mc);
                }
                counter++;
            }
        }
        private void btnAffixFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            if (dlg.ShowDialog() == true)
            {
                AffixFiles = 1;
                affixPath = dlg.FileName;
            }
            else
            {
                AffixFiles = 0;
                affixPath = "";
            }
        }

        private async void btnUserName_Click(object sender, RoutedEventArgs e)
        {
            string name = "";
            do
            {
                name = await DialogManager.ShowInputAsync(this, "Имя пользователя", "", new MetroDialogSettings
                {
                    AffirmativeButtonText = "Изменить",
                    NegativeButtonText = "Отменить",
                    DefaultText = UserName
                });
                if (name == null) break;
                else UserName = name;
            } while (name.Equals(""));
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = tbMsg.Text;
            if (string.IsNullOrWhiteSpace(text)) return;
            string message = await Task.Run(() =>
            {
                string msg = String.Format("{0}: {1};", userName, text);
                senderSocket.SendTo(Encoding.Unicode.GetBytes(msg), broadcastEP);
                return msg;
            });
            tbMsg.Text = "";
            scroll.ScrollToEnd();
        }

    }
}
