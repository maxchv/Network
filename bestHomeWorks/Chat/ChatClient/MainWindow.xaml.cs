using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Socket _clientSocket;
        private const int Port = 10000;
        private readonly IPEndPoint _ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), Port);
        
        public MainWindow()
        {
            InitializeComponent();

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
        }

        private async void ConnectedToServer()
        {
            try
            {
                MessagesTextBox.Text += "root -> Ожидание соединения с сервером\r\n";
                await Task.Factory.FromAsync(_clientSocket.BeginConnect, _clientSocket.EndConnect, _ipEndPoint, null);
                //_clientSocket.Connect(_ipEndPoint);

                if (_clientSocket.Connected)
                {
                    MessagesTextBox.Text += $"root -> Подключение к серверу {_clientSocket.RemoteEndPoint} произошло успешно\r\n";

                    bool isRepeat = true;
                    byte[] buff = new byte[1024];
                    do
                    {
                        string result = await this.ShowInputAsync("Имя пользователя", "Введите имя пользователя:");

                        _clientSocket.Send(Encoding.GetEncoding(1251).GetBytes($"set name {result}"));

                        _clientSocket.Receive(buff);

                        if (Encoding.GetEncoding(1251).GetString(buff).IndexOf("set_name=true", StringComparison.Ordinal) != -1)
                        {
                            isRepeat = false;
                        }
                        else
                        {
                            MessageBox.Show("Указанное имя уже занято другим пользователем!\r\nПожалуйста введите другое имя!",
                                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    } while (isRepeat);

                    _clientSocket.Send(Encoding.GetEncoding(1251).GetBytes("get user list"));

                    ConversationStart();
                    SendMessageButton.IsEnabled = true;
                }
            }
            catch (SocketException exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                MessagesTextBox.Text += "root -> При попытке подключиться к серверу произошла ошибка! Можете произвести повторно попытку подключиться к серверу через соответствующий пункт в главном меню!\r\n";
                SendMessageButton.IsEnabled = false;
            }
        }

        private void ConversationStart()
        {
            Task.Run(() =>
            {
                byte[] buff = new byte[1024];
                int len;
                string msg;

                while (true)
                {
                    msg = "";

                    try
                    {
                        len = _clientSocket.Receive(buff);

                        if (len > 0)
                        {
                            msg = Encoding.GetEncoding(1251).GetString(buff, 0, len);


                            if (msg.IndexOf("ClientsNameList:", StringComparison.Ordinal) != -1)
                            {
                                int startIndex = "ClientsNameList:".Length;
                                msg = msg.Substring(startIndex, msg.Length - startIndex);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    UsersListBox.Items.Clear();
                                    UsersListBox.Items.Add("Всем");

                                    foreach (var userName in msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        UsersListBox.Items.Add(userName);
                                    }
                                    UsersListBox.SelectedIndex = 0;
                                });
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    foreach (var m in msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                                    {
                                        MessagesTextBox.Text += $"[{DateTime.Now.ToLongTimeString()}] {m}\r\n";
                                    }
                                });
                            }
                        }
                    }
                    catch (SocketException)
                    {
                        //MessageBox.Show(exception.Message, "Error 5", MessageBoxButton.OK, MessageBoxImage.Error);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _clientSocket.Close();
                        });
                    }
                }
            });
        }

        private void ButtonSend_OnClick(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void SendMessage()
        {
            try
            {
                if (string.IsNullOrEmpty(MessageTextBox.Text) &&
                    string.IsNullOrWhiteSpace(MessageTextBox.Text))
                {
                    MessageBox.Show("Пожалуйста укажите текст сообщения!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (UsersListBox.SelectedIndex == 0 || UsersListBox.SelectedIndex == -1)
                {
                    string message = $"{MessageTextBox.Text};";
                    _clientSocket.Send(Encoding.GetEncoding(1251).GetBytes(message));
                    MessagesTextBox.Text += $"[{DateTime.Now.ToLongTimeString()}] [you] ->" + message + "\r\n";
                }
                else
                {
                    string userName = UsersListBox.SelectedValue.ToString();
                    string message = $"{MessageTextBox.Text} private message to user={userName};";
                    _clientSocket.Send(Encoding.GetEncoding(1251).GetBytes(message));
                    MessagesTextBox.Text += $"[{DateTime.Now.ToLongTimeString()}] [you] [private message to {userName}] -> " + MessageTextBox.Text + "\r\n";
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearChatButton_Click(object sender, RoutedEventArgs e)
        {
            MessagesTextBox.Text = string.Empty;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                _clientSocket.Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            base.OnClosing(e);
        }

        private void MessageTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectedToServer();
        }
    }
}