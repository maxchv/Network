using System.Windows;

namespace ChatServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Server server = new Server(10000, LogsTextBox);

            server.Run();
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            LogsTextBox.Text = string.Empty;
        }
    }
}