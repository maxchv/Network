using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Windows;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using vtortola.WebSockets;

namespace WebChatServer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private static CancellationTokenSource _tockenSource;
        private List<ChatUser> _users;
        private IPEndPoint _ipEndPoint;
        private WebSocketListener _webSocketListener;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _tockenSource = new CancellationTokenSource();
                _users = new List<ChatUser>();
                _ipEndPoint = new IPEndPoint(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"]),
                    Convert.ToInt32(ConfigurationManager.AppSettings["port"]));
                _webSocketListener = new WebSocketListener(_ipEndPoint);

                var rfc6455 = new vtortola.WebSockets.Rfc6455.WebSocketFactoryRfc6455(_webSocketListener);
                _webSocketListener.Standards.RegisterStandard(rfc6455);

                _webSocketListener.Start();
            }
            catch (Exception exception)
            {
                LogsTextBox.Text += $"[{DateTime.Now}] {exception.Message}\r\n";
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            LogsTextBox.Text = string.Empty;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Listening();
        }

        private async void Listening()
        {
            try
            {
                while (!_tockenSource.Token.IsCancellationRequested)
                {
                    WebSocket webSocket = await _webSocketListener.AcceptWebSocketAsync(_tockenSource.Token);
                    LogsTextBox.Text += $"[{DateTime.Now}] Клиент {webSocket.RemoteEndpoint} успешно подключился\r\n";

                    SetResponce(webSocket);
                }
            }
            catch (Exception exception)
            {
                LogsTextBox.Text += $"[{DateTime.Now}] {exception.Message}\r\n";
            }
        }

        private async void SetResponce(WebSocket ws)
        {
            StringBuilder responseBuilder = new StringBuilder();

            try
            {
                while (ws.IsConnected)
                {
                    string request = await ws.ReadStringAsync(_tockenSource.Token);
                    LogsTextBox.Text += $"[{DateTime.Now}] Запрос: {request}\r\n";

                    if (request.Contains("Connected new user:"))
                    {
                        ChatUser newUser = new ChatUser();
                        newUser.NickName = request.Substring(request.IndexOf(":", StringComparison.Ordinal) + 2);
                        newUser.WebSocket = ws;
                        newUser.Id = GetId(_users);

                        _users.Add(newUser);

                        //responseBuilder.Clear();
                        //responseBuilder.Append($"set_id:{newUser.Id};");
                        //await ws.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                        //LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                        responseBuilder.Clear();
                        responseBuilder.Append("clear_user_list");
                        await ws.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                        LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                        responseBuilder.Clear();
                        responseBuilder.Append("userName:Всем;userId:0");
                        await ws.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                        LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                        foreach (var user in _users)
                        {
                            if (user.WebSocket != ws)
                            {
                                responseBuilder.Clear();
                                responseBuilder.Append($"userName:{user.NickName};userId:{user.Id}");
                                await ws.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";
                            }
                        }

                        foreach (var user in _users)
                        {
                            if (user.WebSocket != ws)
                            {
                                responseBuilder.Clear();
                                responseBuilder.Append("clear_user_list");
                                await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                                responseBuilder.Clear();
                                responseBuilder.Append("userName:Всем;userId:0");
                                await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                                foreach (var user2 in _users)
                                {
                                    if (user2.WebSocket != user.WebSocket)
                                    {
                                        responseBuilder.Clear();
                                        responseBuilder.Append($"userName:{user2.NickName};userId:{user2.Id}");
                                        await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                        LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        string messagePattern = @"ID:(?<ID>\d*);Message:(?<Message>.*)";

                        if (Regex.IsMatch(request, messagePattern))
                        {
                            Match match = Regex.Match(request, messagePattern);

                            int id = Convert.ToInt32(match.Groups["ID"].Value);
                            string message = match.Groups["Message"].Value;

                            if (id == 0)
                            {
                                ChatUser chatUser = GetChatUserByWebSocket(ws, _users);

                                if (chatUser != null)
                                {
                                    foreach (var user in _users)
                                    {
                                        if (user.WebSocket != ws)
                                        {
                                            responseBuilder.Clear();
                                            responseBuilder.Append($"{chatUser.NickName}:{message};");
                                            await user.WebSocket.WriteStringAsync(responseBuilder.ToString(),
                                                _tockenSource.Token);
                                            LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ChatUser senderChatUser = GetChatUserByWebSocket(ws, _users);
                                ChatUser chatUser = GetChatUserById(_users, id);

                                if (chatUser != null && senderChatUser != null)
                                {
                                    responseBuilder.Clear();
                                    responseBuilder.Append($"{senderChatUser.NickName}:{message};");
                                    await chatUser.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                    LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";


                                    //responseBuilder.Clear();
                                    //responseBuilder.Append($"{senderChatUser.NickName}:{message};");
                                    //await senderChatUser.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                    //LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                try
                {
                    if (!ws.IsConnected)
                    {
                        ChatUser chatUser = GetChatUserByWebSocket(ws, _users);

                        if (chatUser != null)
                        {
                            _users.Remove(chatUser);

                            foreach (var user in _users)
                            {
                                responseBuilder.Clear();
                                responseBuilder.Append("clear_user_list");
                                await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                                responseBuilder.Clear();
                                responseBuilder.Append("userName:Всем;userId:0");
                                await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";

                                foreach (var user2 in _users)
                                {
                                    if (user2.WebSocket != user.WebSocket)
                                    {
                                        responseBuilder.Clear();
                                        responseBuilder.Append($"userName:{user2.NickName};userId:{user2.Id}");
                                        await user.WebSocket.WriteStringAsync(responseBuilder.ToString(), _tockenSource.Token);
                                        LogsTextBox.Text += $"[{DateTime.Now}] Ответ: {responseBuilder}\r\n";
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    LogsTextBox.Text += $"[{DateTime.Now}] {e.Message}\r\n";
                }
                LogsTextBox.Text += $"[{DateTime.Now}] {exception.Message}\r\n";
            }
        }

        private static ChatUser GetChatUserByWebSocket(WebSocket webSocket, List<ChatUser> users)
        {
            return users.FirstOrDefault(u => u.WebSocket == webSocket);
        }

        private static ChatUser GetChatUserById(List<ChatUser> users, int id)
        {
            return users.FirstOrDefault(u => u.Id == id);
        }

        private static int GetId(List<ChatUser> users)
        {
            int id = 1;

            foreach (var user in users)
            {
                if (user.Id == id)
                {
                    id++;
                }
            }

            return id;
        }
    }
}
