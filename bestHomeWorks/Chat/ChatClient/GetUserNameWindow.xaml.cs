using System.Windows;

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for GetUserNameWindow.xaml
    /// </summary>
    public partial class GetUserNameWindow
    {
        public string UserName { get; set; }

        public GetUserNameWindow()
        {
            InitializeComponent();
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            UserName = UserNameTextBox.Text;
            DialogResult = true;
        }
    }
}
