using vtortola.WebSockets;

namespace WebChatServer
{
    class ChatUser
    {
        public int Id { get; set; }

        public string NickName { get; set; }

        public WebSocket WebSocket { get; set; }
    }
}