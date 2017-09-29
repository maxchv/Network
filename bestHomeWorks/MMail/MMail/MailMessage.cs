using System;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace MMail
{
    class MailMessage : INotifyPropertyChanged
    {
        private int _messageId;

        public int MessageId
        {
            get { return _messageId; }
            set { _messageId = value; }
        }


        private string _emailFrom;

        public string EmailFrom
        {
            get { return _emailFrom; }
            set
            {
                if (value != _emailFrom)
                {
                    _emailFrom = value;
                    NotifyPropertyChanged("EmailFrom");
                }
            }
        }

        private string _emailTo;

        public string EmailTo
        {
            get { return _emailTo; }
            set
            {
                if (value != _emailTo)
                {
                    _emailTo = value;
                    NotifyPropertyChanged("EmailTo");
                }
            }
        }

        private DateTime _date;

        public DateTime Date
        {
            get { return _date; }
            set
            {
                if (value != _date)
                {
                    _date = value;
                    NotifyPropertyChanged("Date");
                }
            }
        }

        private string _subject;

        public string Subject
        {
            get { return _subject; }
            set
            {
                if (value != _subject)
                {
                    _subject = value;
                    NotifyPropertyChanged("Subject");
                }
            }
        }

        private string _message;

        public string Message
        {
            get { return _message; }
            set
            {
                if (value != _message)
                {
                    _message = value;
                    NotifyPropertyChanged("Message");
                }
            }
        }

        private int _messageSize;

        public int MessageSize
        {
            get { return _messageSize; }
            set
            {
                if (value != _messageSize)
                {
                    _messageSize = value;
                    NotifyPropertyChanged("MessageSize");
                }
            }
        }

        private string _contentType;

        public string ContentType
        {
            get { return _contentType; }
            set
            {
                if (value != _contentType)
                {
                    _contentType = value;
                    NotifyPropertyChanged("ContentType");
                }
            }
        }


        public string RawResponse { get; set; }

        public MailMessage(string rawResponse, int messageSize, int messageId)
        {
            try
            {
                RawResponse = rawResponse;
                MessageSize = messageSize;
                MessageId = messageId;

                string emailFromPattern = @"From:\s(?<EmailFrom>.*?\<.+)\>";
                string emailToPattern = @"To:\s(?<EmailTo>.*?\<.+?)\>";
                string subjectPattern = @"Subject:\s?(?<Subject>.*)";
                string contentType = @"Content-Type:\s(?<ContentType>.+)";
                string dateTimePattern = @"Received:.*\;\s(?<DateTime>.*?)\+.*";

                //string messagePattern = @".+\:.+\r\n(?<Message>.+)\.";

                string[] headerStrings = rawResponse.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder builder = new StringBuilder();
                int index = headerStrings.Length;
                for (int i = 0; i < headerStrings.Length; i++)
                {
                    if (Regex.IsMatch(headerStrings[i], emailFromPattern))
                    {
                        EmailFrom = Regex.Match(headerStrings[i], emailFromPattern).Groups["EmailFrom"].Value.Replace("<", "");
                    }
                    else if (Regex.IsMatch(headerStrings[i], emailToPattern))
                    {
                        EmailTo = Regex.Match(headerStrings[i], emailToPattern).Groups["EmailTo"].Value.Replace("<", "");
                    }
                    else if (Regex.IsMatch(headerStrings[i], dateTimePattern))
                    {
                        Date = DateTime.Parse(Regex.Match(headerStrings[i], dateTimePattern).Groups["DateTime"].Value);
                    }
                    else if (Regex.IsMatch(headerStrings[i], subjectPattern))
                    {
                        Subject = Regex.Match(headerStrings[i], subjectPattern).Groups["Subject"].Value;
                    }
                    else if (Regex.IsMatch(headerStrings[i], contentType))
                    {
                        ContentType = Regex.Match(headerStrings[i], contentType).Groups["ContentType"].Value;
                    }
                    else
                    {
                        if (i > index)
                        {
                            if (!headerStrings[i].Equals("."))
                            {
                                builder.Append(headerStrings[i]);
                            }
                        }
                    }

                    if (i + 1 < headerStrings.Length)
                    {
                        if (headerStrings[i].IndexOf(":", StringComparison.Ordinal) != -1 &&
                            headerStrings[i + 1].IndexOf(":", StringComparison.Ordinal) != -1)
                        {
                            index = i + 1;
                        }
                    }
                }

                Message = builder.ToString();
            }
            catch
            {
                // ignored
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}