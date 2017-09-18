using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatRoom
{
    /// <summary>
    /// Interaction logic for IpPortAddress.xaml
    /// </summary>
    public partial class IpPortAddress : MetroWindow
    {
        public string IP { get; set; }
        public string Port { get; set; }
        public IpPortAddress(string title = "",string ip="")
        {
            InitializeComponent();
            Title += $" \"{title}\"";
            if(!String.IsNullOrWhiteSpace(ip))
            {
                Edit.IsChecked = true;
                IPTextBox.Text = ip;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                IPAddress.Parse((sender as TextBox).Text);
                (sender as TextBox).BorderBrush = new SolidColorBrush(Colors.LightGray);
                IP = (sender as TextBox).Text;
            }
            catch
            {
                (sender as TextBox).BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {
            try
            {
                Int32.Parse((sender as TextBox).Text);
                (sender as TextBox).BorderBrush = new SolidColorBrush(Colors.LightGray);
                Port = (sender as TextBox).Text;
            }
            catch
            {
                (sender as TextBox).BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }

        private void Default_Checked(object sender, RoutedEventArgs e)
        {
            IP = "default";
            IPTextBox.Text = IPAddress.Loopback + "";
        }

        private void Any_Checked(object sender, RoutedEventArgs e)
        {
            IP = "any";
            IPTextBox.Text = IPAddress.Any + "";
        }

        private void DefaultPort_Checked(object sender, RoutedEventArgs e)
        {
            Port = 10001 + "";
            PortTextBox.Text = Port;
        }
    }
}
