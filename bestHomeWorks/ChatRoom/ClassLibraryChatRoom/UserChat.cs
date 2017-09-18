using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibraryChatRoom
{
    public class UserChat : INotifyPropertyChanged
    {
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; NotifyPropertyChanged("Name"); }
        }
        private ObservableCollection<MessageChat> messages = new ObservableCollection<MessageChat>();
        public ObservableCollection<MessageChat> Messages
        {
            get { return messages; }
            set { messages = value; NotifyPropertyChanged("Messages"); }
        }
        private string remoteEndPoint;
        public string RemoteEndPoint
        {
            get { return remoteEndPoint; }
            set { remoteEndPoint = value; NotifyPropertyChanged("RemoteEndPoint"); }
        }
        public void AddMessage(string message)
        {
            var v = TryParseMessage(message);
            if (v != null)
                Messages.Add(v);
        }
        public static MessageChat TryParseMessage(string message)
        {
            MessageChat msg;
            int idxTwoPoint = message.IndexOf(':');
            int length = message.LastIndexOf(';') - (idxTwoPoint + 1);
            if (idxTwoPoint != -1)
            {
                string name = message.Substring(0, idxTwoPoint);
                string msgs = message.Substring(idxTwoPoint + 1, length);
                msg = new MessageChat() { Message = msgs, UserName = name, Time = DateTime.Now };
            }
            else
                msg = new MessageChat() { Message = message, Time = DateTime.Now };
            return msg;
        }
    }
}
