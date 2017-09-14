using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using static System.DateTime;

namespace ChatServer
{
    class Server
    {
        private readonly int _port;
        private readonly Socket _serverSocket;
        private List<Socket> _clientSockets;
        private Dictionary<string, string> _clientsName;
        private TextBox _logsTextBox;

        public Server(int port, TextBox textBox)
        {
            _port = port;
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _port);
            _clientSockets = new List<Socket>();
            _clientsName = new Dictionary<string, string>();
            _logsTextBox = textBox;

            _serverSocket.Bind(ipEndPoint);
            _serverSocket.Listen(10);
        }

        public async void Run()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Сервер слушает порт #{_port}\r\n";
            });


            while (true)
            {
                _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Ждем подключения нового клиента...\r\n";

                Socket newClient = await Task.Factory.FromAsync(_serverSocket.BeginAccept, _serverSocket.EndAccept, true);

                _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Клиент пришел {newClient.RemoteEndPoint}\r\n";

                lock (this)
                {
                    _clientSockets.Add(newClient);
                    _clientsName.Add(newClient.RemoteEndPoint.ToString(), string.Empty);
                }

                ConversationStart(newClient);
            }
        }

        private void SendClientNames(Socket thisClient)
        {
            try
            {
                string names = "ClientsNameList:";

                foreach (Socket otherClient in _clientSockets)
                {
                    if (thisClient != otherClient)
                    {
                        lock (this)
                        {
                            names += _clientsName[otherClient.RemoteEndPoint.ToString()] != string.Empty
                                ? _clientsName[otherClient.RemoteEndPoint.ToString()] + ";" : "";
                        }
                    }
                }
                thisClient.Send(Encoding.GetEncoding(1251).GetBytes(names));
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Отправки списка клиентов клиенту {_clientsName[thisClient.RemoteEndPoint.ToString()]}\r\n";
                });
            }
            catch
            {
                // ignored
            }
        }

        private void ConversationStart(Socket thisClient)
        {
            Task.Run(() =>
            {
                byte[] buff = new byte[1024];
                int len;
                string msg;

                while (true)
                {
                    msg = string.Empty;
                    try
                    {
                        len = thisClient.Receive(buff);

                        if (len > 0)
                        {
                            msg = Encoding.GetEncoding(1251).GetString(buff, 0, len);

                            int startIndex;
                            if (msg.IndexOf("get user list", StringComparison.Ordinal) != -1)
                            {
                                SendClientNames(thisClient);
                                msg = string.Empty;
                            }
                            else if (msg.IndexOf("set name ", StringComparison.Ordinal) != -1)
                            {
                                startIndex = "set name ".Length;
                                string userName = msg.Substring(startIndex, msg.Length - startIndex);

                                string remoteIpPort = _clientsName.FirstOrDefault(t => t.Value.Equals(userName)).Key;

                                if (string.IsNullOrEmpty(remoteIpPort))
                                {
                                    _clientsName[thisClient.RemoteEndPoint.ToString()] = userName;

                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        _logsTextBox.Text +=
                                            $"[{Now.ToLongTimeString()}] Клиенту с RemoteEndPoint = {thisClient.RemoteEndPoint} установленно имя -> {_clientsName[thisClient.RemoteEndPoint.ToString()]}\r\n";
                                    });

                                    thisClient.Send(Encoding.GetEncoding(1251).GetBytes("set_name=true"));
                                }
                                else
                                {
                                    thisClient.Send(Encoding.GetEncoding(1251).GetBytes("set_name=false"));
                                }

                                msg = string.Empty;

                                foreach (Socket otherClient in _clientSockets)
                                {
                                    if (thisClient != otherClient)
                                    {
                                        SendClientNames(otherClient);
                                    }
                                }
                            }
                            else
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Клиент {_clientsName[thisClient.RemoteEndPoint.ToString()]} отправил сообщение {msg}\r\n";
                                });
                            }

                        }
                    }
                    catch (SocketException)
                    {
                        lock (this)
                        {
                            _clientsName.Remove(thisClient.RemoteEndPoint.ToString());
                            _clientSockets.Remove(thisClient);
                        }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Клиент ушел {_clientsName[thisClient.RemoteEndPoint.ToString()]}\r\n";
                        });

                        foreach (Socket otherClient in _clientSockets)
                        {
                            SendClientNames(otherClient);
                        }
                        break;
                    }
                    if (!thisClient.Connected)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _logsTextBox.Text += $"[{Now.ToLongTimeString()}] Клиент отключился {_clientsName[thisClient.RemoteEndPoint.ToString()]}\r\n";
                        });

                        lock (this)
                        {
                            _clientsName.Remove(thisClient.RemoteEndPoint.ToString());
                            _clientSockets.Remove(thisClient);
                        }
                        foreach (Socket otherClient in _clientSockets)
                        {
                            SendClientNames(otherClient);
                        }
                        break;
                    }
                    
                    foreach (var m in msg.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (m.IndexOf("private message to user", StringComparison.Ordinal) != -1)
                        {
                            int start = m.IndexOf("=") + 1;
                            string userName = m.Substring(start, m.Length - start);

                            string remoteIpPort = _clientsName.FirstOrDefault(t => t.Value.Equals(userName)).Key;

                            if (!string.IsNullOrEmpty(remoteIpPort))
                            {
                                Socket clientSocket =
                                    _clientSockets.FirstOrDefault(t => t.RemoteEndPoint.ToString().Equals(remoteIpPort));

                                if (clientSocket != null)
                                {
                                    start = m.IndexOf("private message to user", StringComparison.Ordinal);
                                    clientSocket.Send(Encoding.GetEncoding(1251).GetBytes(
                                        $"[{_clientsName[thisClient.RemoteEndPoint.ToString()]}] [private message] ->" + m.Substring(0, start)));
                                }
                            }

                        }
                        else
                        {
                            foreach (Socket otherClient in _clientSockets)
                            {
                                if (thisClient != otherClient)
                                {
                                    lock (this)
                                    {
                                        otherClient.Send(
                                            Encoding.GetEncoding(1251).GetBytes(
                                                $"[{_clientsName[thisClient.RemoteEndPoint.ToString()]}] ->" + m));
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }
    }
}