using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ClientChatBykova
{
    public class UserChat : INotifyPropertyChanged
    {
        /*
            BackgroundGrid
            UserName
            UserMessage
            UserAlignment
        */

        private int currPort;
        public int CurrPort { get { return currPort; } set { currPort = value; NotifyPropertyChanged("CurrPort"); } }

        private string userId;

        public string UserId
        {
            get { return userId; }
            set { userId = value; NotifyPropertyChanged("UserId"); }
        }

        private string GenerateUserId()
        {
            int[] arr = new int[16]; // сделаем длину пароля в 16 символов
            Random rnd = new Random();
            string id = "";

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = rnd.Next(65, 122);
                id += (char)arr[i];
            }
            return id;
        }

        public UserChat()
        {
            UserId = GenerateUserId();
            UserName = "User";
            UserMessage = "";
            BackgroundGrid = Brushes.LightBlue;
            UserAlignment = HorizontalAlignment.Right;
        }

        public UserChat(string name, string message, bool isCurrUser = false)
        {
            UserId = GenerateUserId();
            UserName = name;
            UserMessage = message;

            if(!isCurrUser)
            {
                BackgroundGrid = Brushes.White;
                UserAlignment = HorizontalAlignment.Left;
            }
            else
            {
                BackgroundGrid = Brushes.LightBlue;
                UserAlignment = HorizontalAlignment.Right;
            }
        }

        private Brush backgroundGrid;

        public Brush BackgroundGrid
        {
            get { return backgroundGrid; }
            set { backgroundGrid = value; NotifyPropertyChanged("BackgroundGrid"); }
        }

        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value; NotifyPropertyChanged("UserName"); }
        }

        private string userMessage;

        public string UserMessage
        {
            get { return userMessage; }
            set { userMessage = value; NotifyPropertyChanged("UserMessage"); }
        }

        private HorizontalAlignment userAlignment;

        public HorizontalAlignment UserAlignment
        {
            get { return userAlignment; }
            set { userAlignment = value; NotifyPropertyChanged("UserAlignment"); }
        }

        

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }

        
    }
}
