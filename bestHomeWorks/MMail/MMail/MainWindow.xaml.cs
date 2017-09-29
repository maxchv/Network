using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace MMail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _login;
        private string _password;
        private bool _isMailDownload;
        private ObservableCollection<MailMessage> _mailMessages;

        public MainWindow()
        {
            _mailMessages = new ObservableCollection<MailMessage>();

            InitializeComponent();
            Loaded += MainWindow_Loaded;

            MessagesListBox.ItemsSource = _mailMessages;
            _login = string.Empty;
            _password = string.Empty;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Authorization();
        }

        private async void Authorization()
        {
            bool authorizationFailed;

            do
            {
                authorizationFailed = false;
                _mailMessages.Clear();

                LoginDialogSettings loginPasswordSettings = new LoginDialogSettings
                {
                    ColorScheme = MetroDialogOptions.ColorScheme,
                    AffirmativeButtonText = "Войти",
                    PasswordWatermark = "Пароль",
                    UsernameWatermark = "Логин",
                    AnimateShow = true,
                    AnimateHide = true,
                    NegativeButtonVisibility = Visibility.Visible,
                    NegativeButtonText = "Отмена"
                };
                LoginDialogData result = await this.ShowLoginAsync("Авторизация", "Введите Ваши учетные данные", loginPasswordSettings);

                if (result == null)
                {
                    LoginDialogSettings messageSettings = new LoginDialogSettings
                    {
                        ColorScheme = MetroDialogOptions.ColorScheme,
                        AffirmativeButtonText = "Да",
                        AnimateShow = true,
                        AnimateHide = true,
                        NegativeButtonVisibility = Visibility.Visible,
                        NegativeButtonText = "Повторить авторизацию"
                    };

                    MessageDialogResult messageResult = await this.ShowMessageAsync("Отмена авторизации",
                        "Вы отменили авторизацию! Не авторизованные пользователи не могут пользоваться программой!\r\n\r\nЗакрыть программу?",
                        MessageDialogStyle.AffirmativeAndNegative, messageSettings);

                    if (messageResult == MessageDialogResult.Affirmative)
                    {
                        Close();
                    }
                    else
                    {
                        authorizationFailed = true;
                        StatusBar.Text = "Ошибка авторизации!";
                    }
                }
                else
                {
                    _login = result.Username;
                    _password = result.Password;
                }

                if (!authorizationFailed)
                {
                    _isMailDownload = true;

                    MailDownload();
                }
            } while (authorizationFailed);
        }

        private void MailDownload()
        {
            ThreadPool.QueueUserWorkItem((arg) =>
            {
                string login = String.Empty;
                string password = String.Empty;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    login = _login;
                    password = _password;
                });

                try
                {
                    TcpClient tcpClient = new TcpClient();
                    NetworkStream netStream;
                    byte[] buffer;
                    StringBuilder serverResponse = new StringBuilder();
                    StringBuilder pairBuilder = new StringBuilder();
                    Dictionary<int, int> messageIdSizeDictionary;
                    int selectedIndex = -1;

                    while (_isMailDownload)
                    {
                        try
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusBar.Text = "Подождите идет загрузка писем с сервера!";
                            });

                            tcpClient = new TcpClient();
                            tcpClient.Connect(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"].ToString()),
                                              Convert.ToInt32(ConfigurationManager.AppSettings["portPOP3"].ToString()));
                            netStream = tcpClient.GetStream();

                            serverResponse.Clear();
                            serverResponse.Append(RequestToString(netStream));

                            if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                            {
                                throw new Exception("Сервер отклонил общение с клиентом");
                            }

                            string loginRequest = $"USER {login}\r\n";
                            buffer = Encoding.UTF8.GetBytes(loginRequest);
                            netStream.Write(buffer, 0, buffer.Length);

                            serverResponse.Clear();
                            serverResponse.Append(RequestToString(netStream));

                            if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                            {
                                throw new Exception("Пользователь с указанным ЛОГИНОМ не найден!");
                            }

                            string passwordRequest = $"PASS {password}\r\n";
                            buffer = Encoding.UTF8.GetBytes(passwordRequest);
                            netStream.Write(buffer, 0, buffer.Length);

                            serverResponse.Clear();
                            serverResponse.Append(RequestToString(netStream));

                            if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                            {
                                throw new Exception("Указанный ПАРОЛЬ неверный!");
                            }

                            string listRequest = "LIST\r\n";
                            buffer = Encoding.UTF8.GetBytes(listRequest);
                            netStream.Write(buffer, 0, buffer.Length);

                            serverResponse.Clear();
                            serverResponse.Append(RequestToString(netStream));

                            if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                            {
                                throw new Exception("При получении СПИСКА ПИСЕМ произошла ошибка!");
                            }

                            messageIdSizeDictionary = ParseList(serverResponse.ToString());

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                selectedIndex = MessagesListBox.SelectedIndex;
                                _mailMessages.Clear();
                            });

                            foreach (KeyValuePair<int, int> pair in messageIdSizeDictionary)
                            {
                                try
                                {
                                    pairBuilder.Clear();
                                    pairBuilder.Append($"RETR {pair.Key}\r\n");
                                    buffer = Encoding.ASCII.GetBytes(pairBuilder.ToString());
                                    netStream.Write(buffer, 0, buffer.Length);

                                    serverResponse.Clear();
                                    serverResponse.Append(RequestToString(netStream));

                                    if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) != -1)
                                    {
                                        MailMessage message = new MailMessage(serverResponse.ToString(), pair.Value, pair.Key);
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            _mailMessages.Add(message);
                                        });
                                    }
                                    else
                                    {
                                        throw new Exception("Ошибка запроса письма!");
                                    }
                                }
                                catch (Exception exception)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        StatusBar.Text = exception.Message;
                                    });
                                }
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessagesListBox.SelectedIndex = selectedIndex;
                                StatusBar.Text = "Загрузка писем успешно завершена!";
                            });
                        }
                        catch (Exception exception)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusBar.Text = exception.Message;

                                if (exception.Message.Equals("Пользователь с указанным ЛОГИНОМ не найден!") ||
                                    exception.Message.Equals("Указанный ПАРОЛЬ неверный!") ||
                                    exception.Message.IndexOf("Подключение не установленно", StringComparison.Ordinal) == -1)
                                {
                                    _isMailDownload = false;
                                    Authorization();
                                }
                            });
                        }

                        Thread.Sleep(59000);
                        tcpClient.Close();
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusBar.Text = string.Empty;
                        });
                    }
                }
                catch (Exception exception)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusBar.Text = exception.Message;
                        Authorization();
                    });
                }
            }, null);
        }
        
        private Dictionary<int, int> ParseList(string messageListResponse)
        {
            Dictionary<int, int> messageIdSizeDictionary = new Dictionary<int, int>();

            try
            {
                string pattern = @"(?<messageId>\d+)\s(?<messageSize>\d+)";
                Regex regex = new Regex(pattern);

                foreach (Match match in regex.Matches(messageListResponse))
                {
                    messageIdSizeDictionary.Add(Convert.ToInt32(match.Groups["messageId"].Value),
                        Convert.ToInt32(match.Groups["messageSize"].Value));
                }
            }
            catch
            {
                // ignored
            }

            return messageIdSizeDictionary;
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

        private void MessagesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemoveBadged.Badge = MessagesListBox.SelectedItems.Count;
        }


        private async void RemoveMailMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            LoginDialogSettings messageSettings = new LoginDialogSettings
            {
                ColorScheme = MetroDialogOptions.ColorScheme,
                AffirmativeButtonText = "Да",
                AnimateShow = true,
                AnimateHide = true,
                NegativeButtonVisibility = Visibility.Visible,
                NegativeButtonText = "Нет"
            };

            MessageDialogResult messageResult = await this.ShowMessageAsync("Удаление писем",
                "Вы действительно хотите удалить выбранные письма?",
                MessageDialogStyle.AffirmativeAndNegative, messageSettings);

            if (messageResult == MessageDialogResult.Negative)
            {
                return;
            }
            
            StatusBar.Text = "Подождите идет удаление сообщений!";
            ThreadPool.QueueUserWorkItem((arg) =>
            {
                try
                {
                    int countMessage = 0;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        countMessage = MessagesListBox.SelectedItems.Count;
                    });

                    if (countMessage > 0)
                    {
                        TcpClient tcpClient;
                        NetworkStream netStream;
                        byte[] buffer;
                        StringBuilder serverResponse = new StringBuilder();
                        StringBuilder resquestBuilder = new StringBuilder();

                        tcpClient = new TcpClient();
                        tcpClient.Connect(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"].ToString()), 
                                          Convert.ToInt32(ConfigurationManager.AppSettings["portPOP3"].ToString()));
                        netStream = tcpClient.GetStream();

                        serverResponse.Clear();
                        serverResponse.Append(RequestToString(netStream));

                        if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                        {
                            throw new Exception("Сервер отклонил общение с клиентом");
                        }

                        string loginRequest = $"USER {_login}\r\n";
                        buffer = Encoding.UTF8.GetBytes(loginRequest);
                        netStream.Write(buffer, 0, buffer.Length);

                        serverResponse.Clear();
                        serverResponse.Append(RequestToString(netStream));

                        if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                        {
                            throw new Exception("Пользователь с указанным ЛОГИНОМ не найден!");
                        }

                        string passwordRequest = $"PASS {_password}\r\n";
                        buffer = Encoding.UTF8.GetBytes(passwordRequest);
                        netStream.Write(buffer, 0, buffer.Length);

                        serverResponse.Clear();
                        serverResponse.Append(RequestToString(netStream));

                        if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) == -1)
                        {
                            throw new Exception("Указанный ПАРОЛЬ неверный!");
                        }

                        IList list = null;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            list = MessagesListBox.SelectedItems;
                        });

                        for (int i = 0; i < list.Count; i++)
                        {
                            try
                            {
                                MailMessage message = list[i] as MailMessage;

                                if (message != null)
                                {
                                    resquestBuilder.Clear();
                                    resquestBuilder.Append($"DELE {message.MessageId}\r\n");
                                    buffer = Encoding.ASCII.GetBytes(resquestBuilder.ToString());
                                    netStream.Write(buffer, 0, buffer.Length);

                                    serverResponse.Clear();
                                    serverResponse.Append(RequestToString(netStream));

                                    if (serverResponse.ToString().IndexOf("+OK", StringComparison.CurrentCultureIgnoreCase) != -1)
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            _mailMessages.Remove(message);
                                        });
                                        i--;
                                    }
                                    else
                                    {
                                        throw new Exception("При попытке удалить сообщение произошла ошибка!");
                                    }
                                }
                            }
                            catch (Exception exception)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    StatusBar.Text = exception.Message;
                                });
                            }
                        }

                        resquestBuilder.Clear();
                        resquestBuilder.Append("QUIT\r\n");
                        buffer = Encoding.ASCII.GetBytes(resquestBuilder.ToString());
                        netStream.Write(buffer, 0, buffer.Length);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusBar.Text = "Удаление завершено!";
                        });

                        tcpClient.Close();
                    }
                }
                catch (Exception exception)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        StatusBar.Text = exception.Message;
                    });
                }
            });

        }

        private void SendingMailMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                SendingMailMessageWindow window = new SendingMailMessageWindow();
                window.ShowDialog();
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private void SendingMailMessageaToAddress_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(EmailFromTextBox.Text))
                {
                    SendingMailMessageWindow window = new SendingMailMessageWindow(EmailFromTextBox.Text);
                    window.ShowDialog();
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }
    }
}