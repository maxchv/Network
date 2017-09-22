using System;
using System.ComponentModel;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;

namespace WebServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        Server _server;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartStopServerButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StartStopServerButton.Content.ToString().Equals("Запустить сервер"))
                {
                    if (Regex.IsMatch(HostTextBox.Text, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                    {
                        StartStopServerButton.Content = "Остановить сервер";
                        HostTextBox.IsEnabled = false;
                        Port.IsEnabled = false;

                        _server = new Server(IPAddress.Parse(HostTextBox.Text), Convert.ToInt32(Port.Value));
                        _server.Logs = Logs;
                        _server.StartServer();
                    }
                    else
                    {
                        MessageBox.Show("Неверный формат хоста!", "Предупреждение",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (StartStopServerButton.Content.ToString().Equals("Остановить сервер"))
                {
                    StartStopServerButton.Content = "Запустить сервер";
                    HostTextBox.IsEnabled = true;
                    Port.IsEnabled = true;

                    if (_server != null)
                    {
                        _server.IsWorkServer = false;
                        _server.StopServer();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            Logs.Text = string.Empty;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_server != null)
            {
                _server.IsWorkServer = false;
                _server.StopServer();
            }
        }
    }
}