using System.Net.Sockets;

namespace Server
{
    enum Status
    {
        Playing,
        NotPlaying
    }

    class Player
    {
        public string ID { get; set; }

        public string NickName { get; set; }

        public Status Status { get; set; }

        public Socket Socket { get; set; }

        public bool YourTurn { get; set; }

        public CheckerColor Color { get; set; }
    }
}