using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Classes
{
    public class MessageInfo
    {
        public string Text { get; set; }
        public string Time { get; set; }
        public string PathToImg { get; set; }

        public MessageInfo()
        {
            PathToImg = "../Images/person.png";
        }
    }
}
