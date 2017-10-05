using System;
using System.Configuration;
using System.Net;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private CentralServer _server;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartServerButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_server != null)
            {
                if (!_server.IsRunServer)
                {
                    _server.RunAsync();
                }
            }
            else
            {
                _server = new CentralServer(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"]),
                                     Convert.ToInt32(ConfigurationManager.AppSettings["port"]),
                                     LogsTextBox);

                _server.RunAsync();
            }
        }
    }
}