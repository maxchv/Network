using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Configuration;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;

namespace MMail
{
    /// <summary>
    /// Interaction logic for SendingMailMessageWindow.xaml
    /// </summary>
    public partial class SendingMailMessageWindow
    {
        public SendingMailMessageWindow()
        {
            InitializeComponent();
        }

        public SendingMailMessageWindow(string email)
        {
            InitializeComponent();

            EmailToTextBox.Text = email;
        }

        private void SendMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private async void SendMessage()
        {
            try
            {
                StringBuilder serverResponse = new StringBuilder();
                StringBuilder requestBuilder = new StringBuilder();
                TcpClient tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"]),
                                            Convert.ToInt32(ConfigurationManager.AppSettings["portSMTP"]));

                NetworkStream netStream = tcpClient.GetStream();

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("220", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Сервер отклонил общение с клиентом");
                }

                requestBuilder.Clear();
                requestBuilder.Append("HELO\r\n");
                byte[] bufferTmp = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("250", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Сервер отклонил общение с клиентом");
                }

                requestBuilder.Clear();
                requestBuilder.Append($"MAIL FROM: <{EmailFromTextBox.Text}>\r\n");
                bufferTmp = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("250", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Ошибка неверное значение \"От кого\"!");
                }

                requestBuilder.Clear();
                requestBuilder.Append($"RCPT TO: <{EmailToTextBox.Text}>\r\n");
                bufferTmp = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("250", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Ошибка неверное значение \"Кому\"!");
                }

                requestBuilder.Clear();
                requestBuilder.Append("DATA\r\n");
                bufferTmp = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("354", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Сервер не захотел принимать данные!");
                }

                List<string> requestsList = new List<string>()
                {
                    $"From: {EmailFromNameTextBox.Text} <{EmailFromTextBox.Text}>\r\n",
                    $"To: {EmailToNameTextBox.Text} <{EmailToTextBox.Text}>\r\n",
                    $"Subject: {SubjectTextBox.Text}\r\n",
                    "Content-Type: text/plain\r\n",
                    "\r\n",
                    $"{MessageTextBox.Text}\r\n",
                    ".\r\n"
                };

                foreach (string data in requestsList)
                {
                    bufferTmp = Encoding.UTF8.GetBytes(data);
                    await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);
                }

                serverResponse.Clear();
                serverResponse.Append(RequestToString(netStream));

                if (serverResponse.ToString().IndexOf("250", StringComparison.CurrentCultureIgnoreCase) == -1)
                {
                    throw new Exception("Сервер не принял письмо!");
                }

                requestBuilder.Clear();
                requestBuilder.Append("QUIT\r\n");
                bufferTmp = Encoding.UTF8.GetBytes(requestBuilder.ToString());
                await netStream.WriteAsync(bufferTmp, 0, bufferTmp.Length);
                
                tcpClient.Close();

                await this.ShowMessageAsync("Отправка сообщения", "Сообщение было успешно отправленно!");
            }
            catch (Exception exception)
            {
                await this.ShowMessageAsync("Отправка сообщения", $"Сообщение не было отправленно, так как при отправки возникла ошибка!\r\n\r\n{exception.Message}");
            }
        }

        private string RequestToString(NetworkStream netStream)
        {
            int readBytes;
            byte[] buffer = new byte[512];
            StringBuilder builder = new StringBuilder();
            NetworkStream stream = netStream;

            do
            {
                readBytes = stream.Read(buffer, 0, buffer.Length);
                builder.Append(Encoding.ASCII.GetString(buffer, 0, readBytes));
            } while (stream.DataAvailable);

            return builder.ToString();
        }

        private async void MessageTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers != ModifierKeys.Shift &&
                e.Key == Key.Enter)
            {
                if (!string.IsNullOrEmpty(MessageTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(MessageTextBox.Text))
                {
                    SendMessage();
                }
                else
                {
                    await this.ShowMessageAsync("Предупреждение", "Вы не можете отправить пустое сообщение!");
                }
            }
        }
    }
}