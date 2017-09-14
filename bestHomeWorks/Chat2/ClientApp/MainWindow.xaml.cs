using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.ObjectModel;

namespace ClientApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public class MsgInfo
    {
        public string Text { get; set; }
        public string Time { get; set; }

        public HorizontalAlignment Alignment { get; set; }
    }
    public partial class MainWindow : MetroWindow
    {
        public User Person { get; set; }

        public ObservableCollection<MsgInfo> Msgs = new ObservableCollection<MsgInfo>();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            lbMsgs.ItemsSource = Msgs;
            scroll.ScrollToEnd();
            Person = new User
            {
                Name = $"User"
            };
            lblUsername.Content = Person.Name;
            CheckAvailableConnection();
        }

        private async void CheckAvailableConnection()
        {
            if (!Person.Socket.Connected)
            {
               
                    lblStatus.Content = "Подключение отсутствует";
                    btnSend.IsEnabled = false;
                    btnRestartConnection.IsEnabled = true;
                    btnChangeName.IsEnabled = false;
                
            }
            else
            {                
                    lblStatus.Content = "Подключен";
                    btnRestartConnection.IsEnabled = false;
                    btnChangeName.IsEnabled = true;
                
                await Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            byte[] buff = new byte[255];
                            int len = Person.Socket.Receive(buff);
                            string text = Encoding.UTF8.GetString(buff, 0, len);
                            MsgInfo mi = new MsgInfo
                            {
                                Alignment = HorizontalAlignment.Left,
                                Text = text,
                                Time = DateTime.Now.ToString()
                            };
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                Msgs.Add(mi);
                                scroll.ScrollToEnd();
                            }));
                        }
                        catch (Exception)
                        {
                            CheckAvailableConnection();
                        }
                    }
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Person.Socket.Close();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (Person.Socket.Connected)
            {
                if (!string.IsNullOrWhiteSpace(tbMsg.Text))
                {
                    MsgInfo msg = new MsgInfo
                    {
                        Alignment = HorizontalAlignment.Right,
                        Text = tbMsg.Text,
                        Time = DateTime.Now.ToString()
                    };
                    try
                    {
                        Person.Socket.Send(Encoding.UTF8.GetBytes($"({Person.Name}){tbMsg.Text}"));
                        Msgs.Add(msg);
                        scroll.ScrollToEnd();
                    }
                    catch (Exception ex)
                    {
                        CheckAvailableConnection();
                        MessageBox.Show(ex.Message);
                    }
                    tbMsg.Text = "";
                }
            }
            else { CheckAvailableConnection(); }
        }

        private void tbMsg_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Person.Socket.Connected)
            {
                if (string.IsNullOrWhiteSpace(tbMsg.Text)) btnSend.IsEnabled = false;
                else
                {
                    btnSend.IsEnabled = true;
                }
            }
            else
            {
                CheckAvailableConnection();
            }
        }

        private async void btnRestartConnection_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                Person.Connection();
                CheckAvailableConnection();
            });
        }

        private async void changeName_Click(object sender, RoutedEventArgs e)
        {
            string name = await DialogManager.ShowInputAsync(this, "Введите свое имя", "", new MetroDialogSettings
            {
                AffirmativeButtonText = "Подтвердить",
                NegativeButtonText = "Отменить"
            });
            if (string.IsNullOrWhiteSpace(name))
            {
                await DialogManager.ShowMessageAsync(this, "Недопустимое имя", "");
                return;
            }
            Person.Name = name;
            lblUsername.Content = name;
        }
    }
}
