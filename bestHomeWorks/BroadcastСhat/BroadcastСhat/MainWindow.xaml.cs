using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BroadcastСhat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Socket _senderSocket;
        private Socket _receiverSocket;
        private EndPoint _broadcastEndPoint;
        private EndPoint _remoteEndPoint;
        private bool _isAlive;
        private int _port;

        public MainWindow()
        {
            InitializeComponent();
            
            SendButton.IsEnabled = false;

            _port = Convert.ToInt32(PortNumericUpDown.Value);
        }

        private void PortNumericUpDown_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            _port = Convert.ToInt32(PortNumericUpDown.Value);
        }

        private void SignInButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SignInButton.Content.ToString().Equals("Войти"))
            {
                if (!string.IsNullOrEmpty(UsernameTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    InitializeChat();

                    SendMessage($"{UsernameTextBox.Text}: вошел(-ла) в чат;");

                    SignInButton.Content = "Выйти";
                    SendButton.IsEnabled = true;
                    UsernameTextBox.IsReadOnly = true;
                    PortNumericUpDown.IsEnabled = false;
                }
                else
                {
                    MessageBox.Show("Пожалуйста укажите никнейм!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else if (SignInButton.Content.ToString().Equals("Выйти"))
            {
                ExitChat();
            }
        }

        private void InitializeChat()
        {
            _senderSocket = new Socket(AddressFamily.InterNetwork,
                                       SocketType.Dgram,
                                       ProtocolType.Udp);

            _senderSocket.SetSocketOption(SocketOptionLevel.Socket,
                                          SocketOptionName.Broadcast, 1);
            _senderSocket.EnableBroadcast = true;

            _receiverSocket = new Socket(AddressFamily.InterNetwork,
                                         SocketType.Dgram,
                                         ProtocolType.Udp);

            _broadcastEndPoint = new IPEndPoint(IPAddress.Parse("10.2.124.255"), _port);
            _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            _isAlive = true;

            ReceiveMessage();
        }

        private void ClearMessagesButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessagesTextBox.Text = string.Empty;
        }

        private void SendMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(MessageTextBox.Text) &&
                !string.IsNullOrWhiteSpace(MessageTextBox.Text))
            {
                SendMessage($"{UsernameTextBox.Text}: {MessageTextBox.Text};");
            }
            else
            {
                MessageBox.Show("Вы не можете отправить пустое сообщение!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MessageTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Shift &&
                e.Key == Key.Enter &&
                SendButton.IsEnabled)
            {
                if (!string.IsNullOrEmpty(MessageTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(MessageTextBox.Text))
                {
                    SendMessage($"{UsernameTextBox.Text}: {MessageTextBox.Text};");
                }
                else
                {
                    MessageBox.Show("Вы не можете отправить пустое сообщение!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private async void SendMessage(string message)
        {
            try
            {
                byte[] buffer = Encoding.Unicode.GetBytes(message);

                await Task.Factory.FromAsync(
                    _senderSocket.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, _broadcastEndPoint, null, null),
                    _senderSocket.EndSendTo);

                MessageTextBox.Text = string.Empty;
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error (SendMessage)",
                             MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ReceiveMessage()
        {
            try
            {
                EndPoint localEndPoint = new IPEndPoint(IPAddress.Any, _port);
                _receiverSocket.Bind(localEndPoint);

                byte[] buffer = new byte[512];
                int numberBytesReceived;

                while (_isAlive)
                {
                    try
                    {
                        numberBytesReceived =
                            await Task.Factory.FromAsync(
                                _receiverSocket.BeginReceiveFrom(buffer, 
                                0, buffer.Length, SocketFlags.None, ref _remoteEndPoint, null, null),
                                ias => _receiverSocket.EndReceiveFrom(ias, ref _remoteEndPoint));

                        string messages = Encoding.Unicode.GetString(buffer, 0, numberBytesReceived);

                        foreach (string message in messages.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            MessagesTextBox.Text += message + "\r\n";
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error (ReceiveMessage 1)",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExitChat()
        {
            try
            {
                string message = $"{UsernameTextBox.Text}: покидает чат;";
                byte[] buffer = Encoding.Unicode.GetBytes(message);

                await Task.Factory.FromAsync(
                    _senderSocket.BeginSendTo(buffer, 0, buffer.Length, SocketFlags.None, _broadcastEndPoint, null, null),
                    _senderSocket.EndSendTo);

                _isAlive = false;
                SignInButton.Content = "Войти";
                SendButton.IsEnabled = false;
                PortNumericUpDown.IsEnabled = true;

                _senderSocket.Close();
                _receiverSocket.Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error (ExitChat)",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isAlive)
            {
                ExitChat();
            }
        }
    }
}