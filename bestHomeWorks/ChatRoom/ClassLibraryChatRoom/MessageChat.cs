using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryChatRoom
{
    public class MessageChat : INotifyPropertyChanged
    {
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private string message;

        public string Message
        {
            get { return message; }
            set { message = value; NotifyPropertyChanged("Message"); }
        }
        private DateTime time;
        public DateTime Time
        {
            get { return time; }
            set { time = value; NotifyPropertyChanged("Time"); }
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { userName = value; NotifyPropertyChanged("UserName"); }
        }

    }
}
