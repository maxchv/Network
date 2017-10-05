using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Server
{
    public enum TypeIllumination
    {
        Full,
        OnlyStroke,
        Hide
    }

    class CentralServer
    {
        private readonly int _port;
        private readonly Socket _serverSocket;
        private List<Socket> _clientSockets;
        private CancellationTokenSource _tockenSource;
        private List<Player> _players;
        private TextBox _logsTextBox;
        private List<Game> _games;
        private const int SizeBoard = 8;

        public bool IsRunServer { get; private set; }

        public CentralServer(IPAddress iPAddress, int port, TextBox textBox)
        {
            _logsTextBox = textBox;

            _port = port;
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            var ipEndPoint = new IPEndPoint(iPAddress, _port);

            _clientSockets = new List<Socket>();

            _serverSocket.Bind(ipEndPoint);
            _serverSocket.Listen(10);

            _tockenSource = new CancellationTokenSource();

            _players = new List<Player>();
            _games = new List<Game>();

            IsRunServer = false;
        }

        public async void RunAsync()
        {
            try
            {
                _logsTextBox.Text += $"[{DateTime.Now}] Сервер слушает порт #{_port}\r\n";

                IsRunServer = true;

                while (!_tockenSource.Token.IsCancellationRequested)
                {
                    _logsTextBox.Text += $"[{DateTime.Now}] Ждем подключения нового клиента...\r\n";

                    Socket playerSocket = await Task.Factory.FromAsync(_serverSocket.BeginAccept, _serverSocket.EndAccept, true);

                    _logsTextBox.Text += $"[{DateTime.Now}] Клиент пришел {playerSocket.RemoteEndPoint}\r\n";

                    lock (this)
                    {
                        _clientSockets.Add(playerSocket);

                        Player player = new Player();
                        player.ID = playerSocket.RemoteEndPoint.ToString();
                        player.Socket = playerSocket;
                        player.Status = Status.NotPlaying;

                        _players.Add(player);
                    }

                    ConversationStart(playerSocket);
                }
            }
            catch (Exception)
            {

            }

            IsRunServer = false;
        }

        private async void ConversationStart(Socket playerSocket)
        {
            playerSocket.ReceiveBufferSize = 100000;
            byte[] buffer = new byte[1024];
            int readBytes;
            string message = string.Empty;

            while (!_tockenSource.Token.IsCancellationRequested)
            {
                try
                {
                    SocketError error;
                    readBytes = await Task.Factory.FromAsync(playerSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, out error, null, null),
                                        result => playerSocket.EndReceive(result, out error));

                    message += Encoding.GetEncoding(1251).GetString(buffer, 0, readBytes);

                    if (message.IndexOf('$') == -1 && !string.IsNullOrEmpty(message))
                        continue;

                    foreach (string msg in message.Split('$'))
                    {
                        if (Regex.IsMatch(msg, @"set_name:(?<NickName>.*)"))
                        {
                            string nickName = Regex.Match(msg, @"set_name:(?<NickName>.*)")
                                .Groups["NickName"]
                                .Value;

                            Player player =
                                _players.FirstOrDefault(p => p.ID.Equals(playerSocket.RemoteEndPoint.ToString()));

                            if (player != null)
                            {
                                player.NickName = nickName;
                            }
                        }
                        else if (msg.Contains("Start_Game"))
                        {
                            Game game = _games.FirstOrDefault(g => g.Status == GameStatus.WaitingOpponent);

                            if (game != null)
                            {
                                Player player =
                                    _players.FirstOrDefault(p => p.ID.Equals(playerSocket.RemoteEndPoint.ToString()));
                                player.YourTurn = false;
                                player.Color = CheckerColor.Black;

                                game.Player2 = player;
                                game.Status = GameStatus.Goes;

                                string[] messages = new[]
                                {
                                    $"set_oponent_nickname({game.Player1.NickName};Type:{0})$",
                                    $"set_oponent_nickname({game.Player2.NickName};Type:{1})$"
                                };

                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(messages[0]), game.Player1);
                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(messages[1]), game.Player1);

                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(messages[0]), game.Player2);
                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(messages[1]), game.Player2);

                                GetSetAccount(game);

                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"set_checker_color:{CheckerColor.White}$"), game.Player1);
                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"set_checker_color:{CheckerColor.Black}$"), game.Player2);

                                foreach (Checker checker in game.Checkers)
                                {
                                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(checker.ToString()), game.Player1);
                                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes(checker.ToString()), game.Player2);
                                }

                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"your_turn:{game.Player1.YourTurn}$"), game.Player1);
                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"your_turn:{game.Player2.YourTurn}$"), game.Player2);
                            }
                            else
                            {
                                game = new Game();

                                Player player =
                                    _players.FirstOrDefault(p => p.ID.Equals(playerSocket.RemoteEndPoint.ToString()));
                                player.YourTurn = true;
                                player.Color = CheckerColor.White;

                                game.Player1 = player;
                                game.Status = GameStatus.WaitingOpponent;

                                _games.Add(game);

                                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("info:Ожидание опонента$"), game.Player1);
                            }
                        }
                        else if (Regex.IsMatch(msg, @"stroke_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)"))
                        {
                            Match match = Regex.Match(msg,
                                @"stroke_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            Player player = _players.FirstOrDefault(p => p.Socket == playerSocket);

                            if (player != null)
                            {
                                Game game = _games.FirstOrDefault(g =>
                                    (g.Player1 == player || g.Player2 == player) && g.Status == GameStatus.Goes);

                                if (game != null)
                                {
                                    Checker checker = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                    if (checker != null)
                                    {
                                        BacklightingStrokes(game, checker, player, TypeIllumination.Full, false, false);
                                    }
                                }
                            }
                        }
                        else if (Regex.IsMatch(msg, @"movement_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"movement_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            Player player = _players.FirstOrDefault(p => p.Socket == playerSocket);

                            if (player != null)
                            {
                                Game game = _games.FirstOrDefault(g =>
                                    (g.Player1 == player || g.Player2 == player) && g.Status == GameStatus.Goes);

                                if (game != null)
                                {
                                    StrokeCheck(game, row, col, player);
                                }
                            }
                        }
                        else if (msg.Equals("i_give_up"))
                        {
                            Player player = _players.FirstOrDefault(p => p.Socket == playerSocket);

                            if (player != null)
                            {
                                Game game = _games.FirstOrDefault(g =>
                                    (g.Player1 == player || g.Player2 == player) && g.Status == GameStatus.Goes);

                                if (game != null)
                                {
                                    List<Checker> checkers = game.Checkers.Where(c => c.Color == player.Color).ToList();

                                    if (checkers != null)
                                    {
                                        foreach (var checker in checkers)
                                        {
                                            game.Checkers.Remove(checker);
                                        }

                                        GameEnd(game, game.Checkers.Where(c => c.Color == CheckerColor.White).ToList().Count,
                                            game.Checkers.Where(c => c.Color == CheckerColor.Black).ToList().Count);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    break;
                }

                message = string.Empty;
            }
        }

        private async void SendMessagePlayer(byte[] buffer, Player player)
        {
            if (buffer != null && player != null)
            {
                await Task.Factory.FromAsync(
                    player.Socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    res => player.Socket.EndSend(res));
            }
        }

        private void StrokeCheck(Game game, int newRow, int newCol, Player player)
        {
            if (!player.YourTurn)
            {
                return;
            }

            Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == newRow && c.Col == newCol);
            if (c1 != null)
            {
                return;
            }

            if (game.CurrentChecker.Color != player.Color)
            {
                return;
            }

            // если начальная строка больше конечной строки то мы движемся вверх
            if (game.CurrentChecker.Row > newRow)
            {
                // если начальный столбик больше конечного столбика то мы двигаемся влево
                if (game.CurrentChecker.Col > newCol)
                {
                    int offsetRow = (game.CurrentChecker.Row - newRow) / 2;
                    int offsetCol = (game.CurrentChecker.Col - newCol) / 2;

                    if (offsetRow == offsetCol && offsetRow == 0)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == oldRow - 1 && c.Col == oldCol - 1);
                        if (c2 == null)
                        {
                            game.CurrentChecker.Row = oldRow - 1;
                            game.CurrentChecker.Col = oldCol - 1;

                            BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                            СhangePositionChecker(game, oldRow, oldCol);
                            ChangeTurnPlayers(game);

                            IsSetChesckerKing(game);
                        }
                    }
                    else if (offsetRow == offsetCol && offsetRow == 1)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        int removeCheckerRow = oldRow - 1;
                        int removeCheckerCol = oldCol - 1;

                        int newCheckerRow = oldRow - 2;
                        int newCheckerCol = oldCol - 2;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == removeCheckerRow && c.Col == removeCheckerCol);
                        if (c2 != null)
                        {
                            if (c2.Color != player.Color)
                            {
                                Checker c3 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                if (c3 == null)
                                {
                                    game.CurrentChecker.Row = newCheckerRow;
                                    game.CurrentChecker.Col = newCheckerCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);
                                    RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                    IsSetChesckerKing(game);

                                    // если шашка была побита проверка возможен ли еще ход
                                    Checker c4 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                    if (c4 != null)
                                    {
                                        if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                        {
                                            ChangeTurnPlayers(game);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // иначе направо
                else
                {
                    // новые - старые

                    int offsetRow = (game.CurrentChecker.Row - newRow) / 2;
                    int offsetCol = (newCol - game.CurrentChecker.Col) / 2;

                    if (offsetRow == offsetCol && offsetRow == 0)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == oldRow - 1 && c.Col == oldCol + 1);
                        if (c2 == null)
                        {
                            game.CurrentChecker.Row = oldRow - 1;
                            game.CurrentChecker.Col = oldCol + 1;

                            BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                            СhangePositionChecker(game, oldRow, oldCol);
                            ChangeTurnPlayers(game);
                            IsSetChesckerKing(game);
                        }
                    }
                    else if (offsetRow == offsetCol && offsetRow == 1)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        int removeCheckerRow = oldRow - 1;
                        int removeCheckerCol = oldCol + 1;

                        int newCheckerRow = oldRow - 2;
                        int newCheckerCol = oldCol + 2;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == removeCheckerRow && c.Col == removeCheckerCol);
                        if (c2 != null)
                        {
                            if (c2.Color != player.Color)
                            {
                                Checker c3 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                if (c3 == null)
                                {
                                    game.CurrentChecker.Row = newCheckerRow;
                                    game.CurrentChecker.Col = newCheckerCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);
                                    RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                    IsSetChesckerKing(game);

                                    // если шашка была побита проверка возможен ли еще ход
                                    Checker c4 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                    if (c4 != null)
                                    {
                                        if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                        {
                                            ChangeTurnPlayers(game);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // иначе мы движемся вниз
            else
            {
                // если начальный столбик больше конечного столбика то мы двигаемся влево
                if (game.CurrentChecker.Col > newCol)
                {
                    int offsetRow = (newRow - game.CurrentChecker.Row) / 2;
                    int offsetCol = (game.CurrentChecker.Col - newCol) / 2;

                    if (offsetRow == offsetCol && offsetRow == 0)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == oldRow + 1 && c.Col == oldCol - 1);
                        if (c2 == null)
                        {
                            game.CurrentChecker.Row = oldRow + 1;
                            game.CurrentChecker.Col = oldCol - 1;

                            BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                            СhangePositionChecker(game, oldRow, oldCol);
                            ChangeTurnPlayers(game);
                            IsSetChesckerKing(game);
                        }
                    }
                    else if (offsetRow == offsetCol && offsetRow == 1)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        int removeCheckerRow = oldRow + 1;
                        int removeCheckerCol = oldCol - 1;

                        int newCheckerRow = oldRow + 2;
                        int newCheckerCol = oldCol - 2;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == removeCheckerRow && c.Col == removeCheckerCol);
                        if (c2 != null)
                        {
                            if (c2.Color != player.Color)
                            {
                                Checker c3 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                if (c3 == null)
                                {
                                    game.CurrentChecker.Row = newCheckerRow;
                                    game.CurrentChecker.Col = newCheckerCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);
                                    RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                    IsSetChesckerKing(game);

                                    // если шашка была побита проверка возможен ли еще ход
                                    Checker c4 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                    if (c4 != null)
                                    {
                                        if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                        {
                                            ChangeTurnPlayers(game);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // иначе направо
                else
                {
                    // новые - старые

                    int offsetRow = (newRow - game.CurrentChecker.Row) / 2;
                    int offsetCol = (newCol - game.CurrentChecker.Col) / 2;

                    if (offsetRow == offsetCol && offsetRow == 0)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == oldRow + 1 && c.Col == oldCol + 1);
                        if (c2 == null)
                        {
                            game.CurrentChecker.Row = oldRow + 1;
                            game.CurrentChecker.Col = oldCol + 1;

                            BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                            СhangePositionChecker(game, oldRow, oldCol);
                            ChangeTurnPlayers(game);
                            IsSetChesckerKing(game);
                        }
                    }
                    else if (offsetRow == offsetCol && offsetRow == 1)
                    {
                        int oldRow = game.CurrentChecker.Row;
                        int oldCol = game.CurrentChecker.Col;

                        int removeCheckerRow = oldRow + 1;
                        int removeCheckerCol = oldCol + 1;

                        int newCheckerRow = oldRow + 2;
                        int newCheckerCol = oldCol + 2;

                        Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == removeCheckerRow && c.Col == removeCheckerCol);
                        if (c2 != null)
                        {
                            if (c2.Color != player.Color)
                            {
                                Checker c3 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                if (c3 == null)
                                {
                                    game.CurrentChecker.Row = newCheckerRow;
                                    game.CurrentChecker.Col = newCheckerCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);
                                    RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                    IsSetChesckerKing(game);

                                    // если шашка была побита проверка возможен ли еще ход
                                    Checker c4 = game.Checkers.FirstOrDefault(c => c.Row == newCheckerRow && c.Col == newCheckerCol);

                                    if (c4 != null)
                                    {
                                        if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                        {
                                            ChangeTurnPlayers(game);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // дамка
            if (game.CurrentChecker.Type == CheckerType.King)
            {
                // если начальная строка больше конечной строки то мы движемся вверх
                if (game.CurrentChecker.Row > newRow)
                {
                    // если начальный столбик больше конечного столбика то мы двигаемся влево
                    if (game.CurrentChecker.Col > newCol)
                    {
                        // диагональ вверх на лево
                        for (int r = game.CurrentChecker.Row - 1, c = game.CurrentChecker.Col - 1; r >= newRow && c >= newCol; --r, --c)
                        {
                            Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                            if (c2 != null)
                            {
                                if (c2.Color != player.Color)
                                {
                                    int row = --r;
                                    int col = --c;

                                    Checker c3 = game.Checkers.FirstOrDefault(ch => ch.Row == row && ch.Col == col);

                                    if (c3 == null && r == newRow && c == newCol)
                                    {
                                        // шашка побита

                                        int oldRow = game.CurrentChecker.Row;
                                        int oldCol = game.CurrentChecker.Col;

                                        int removeCheckerRow = c2.Row;
                                        int removeCheckerCol = c2.Col;

                                        int newCheckerRow = r;
                                        int newCheckerCol = c;

                                        game.CurrentChecker.Row = newCheckerRow;
                                        game.CurrentChecker.Col = newCheckerCol;

                                        BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                        СhangePositionChecker(game, oldRow, oldCol);
                                        RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                        // если шашка была побита проверка возможен ли еще ход
                                        Checker c4 =
                                            game.Checkers.FirstOrDefault(
                                                ch => ch.Row == newCheckerRow && ch.Col == newCheckerCol);

                                        if (c4 != null)
                                        {
                                            if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                            {
                                                ChangeTurnPlayers(game);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (r == newRow && c == newCol)
                                {
                                    int oldRow = game.CurrentChecker.Row;
                                    int oldCol = game.CurrentChecker.Col;

                                    game.CurrentChecker.Row = newRow;
                                    game.CurrentChecker.Col = newCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);

                                    ChangeTurnPlayers(game);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        // диагональ вверх на право
                        for (int r = game.CurrentChecker.Row - 1, c = game.CurrentChecker.Col + 1;
                            r >= newRow && c <= newCol;
                            --r, ++c)
                        {
                            Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                            if (c2 != null)
                            {
                                if (c2.Color != player.Color)
                                {
                                    int row = --r;
                                    int col = ++c;

                                    Checker c3 = game.Checkers.FirstOrDefault(ch => ch.Row == row && ch.Col == col);

                                    if (c3 == null && r == newRow && c == newCol)
                                    {
                                        // шашка побита

                                        int oldRow = game.CurrentChecker.Row;
                                        int oldCol = game.CurrentChecker.Col;

                                        int removeCheckerRow = c2.Row;
                                        int removeCheckerCol = c2.Col;

                                        int newCheckerRow = r;
                                        int newCheckerCol = c;

                                        game.CurrentChecker.Row = newCheckerRow;
                                        game.CurrentChecker.Col = newCheckerCol;

                                        BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                        СhangePositionChecker(game, oldRow, oldCol);
                                        RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                        // если шашка была побита проверка возможен ли еще ход
                                        Checker c4 =
                                            game.Checkers.FirstOrDefault(
                                                ch => ch.Row == newCheckerRow && ch.Col == newCheckerCol);

                                        if (c4 != null)
                                        {
                                            if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                            {
                                                ChangeTurnPlayers(game);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (r == newRow && c == newCol)
                                {
                                    int oldRow = game.CurrentChecker.Row;
                                    int oldCol = game.CurrentChecker.Col;

                                    game.CurrentChecker.Row = newRow;
                                    game.CurrentChecker.Col = newCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);

                                    ChangeTurnPlayers(game);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // если начальный столбик больше конечного столбика то мы двигаемся влево
                    if (game.CurrentChecker.Col > newCol)
                    {
                        // диагональ вниз на лево
                        for (int r = game.CurrentChecker.Row + 1, c = game.CurrentChecker.Col - 1;
                            r < newRow && c >= newCol;
                            ++r, --c)
                        {
                            Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                            if (c2 != null)
                            {
                                if (c2.Color != player.Color)
                                {
                                    int row = ++r;
                                    int col = --c;

                                    Checker c3 = game.Checkers.FirstOrDefault(ch => ch.Row == row && ch.Col == col);

                                    if (c3 == null)
                                    {
                                        // шашка побита

                                        int oldRow = game.CurrentChecker.Row;
                                        int oldCol = game.CurrentChecker.Col;

                                        int removeCheckerRow = c2.Row;
                                        int removeCheckerCol = c2.Col;

                                        int newCheckerRow = r;
                                        int newCheckerCol = c;

                                        game.CurrentChecker.Row = newCheckerRow;
                                        game.CurrentChecker.Col = newCheckerCol;

                                        BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                        СhangePositionChecker(game, oldRow, oldCol);
                                        RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                        // если шашка была побита проверка возможен ли еще ход
                                        Checker c4 =
                                            game.Checkers.FirstOrDefault(
                                                ch => ch.Row == newCheckerRow && ch.Col == newCheckerCol);

                                        if (c4 != null)
                                        {
                                            if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                            {
                                                ChangeTurnPlayers(game);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (r == newRow && c == newCol)
                                {
                                    int oldRow = game.CurrentChecker.Row;
                                    int oldCol = game.CurrentChecker.Col;

                                    game.CurrentChecker.Row = newRow;
                                    game.CurrentChecker.Col = newCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);

                                    ChangeTurnPlayers(game);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        // диагональ вниз на право
                        for (int r = game.CurrentChecker.Row + 1, c = game.CurrentChecker.Col + 1;
                            r < newRow && c < newCol;
                            ++r, ++c)
                        {
                            Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                            if (c2 != null)
                            {
                                if (c2.Color != player.Color)
                                {
                                    int row = ++r;
                                    int col = ++c;
                                    Checker c3 = game.Checkers.FirstOrDefault(ch => ch.Row == row && ch.Col == col);

                                    if (c3 == null)
                                    {
                                        // шашка побита

                                        int oldRow = game.CurrentChecker.Row;
                                        int oldCol = game.CurrentChecker.Col;

                                        int removeCheckerRow = c2.Row;
                                        int removeCheckerCol = c2.Col;

                                        int newCheckerRow = r;
                                        int newCheckerCol = c;

                                        game.CurrentChecker.Row = newCheckerRow;
                                        game.CurrentChecker.Col = newCheckerCol;

                                        BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                        СhangePositionChecker(game, oldRow, oldCol);
                                        RemoveChecker(game, removeCheckerRow, removeCheckerCol);

                                        // если шашка была побита проверка возможен ли еще ход
                                        Checker c4 =
                                            game.Checkers.FirstOrDefault(
                                                ch => ch.Row == newCheckerRow && ch.Col == newCheckerCol);

                                        if (c4 != null)
                                        {
                                            if (!BacklightingStrokes(game, c4, player, TypeIllumination.OnlyStroke, false, true))
                                            {
                                                ChangeTurnPlayers(game);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                if (r == newRow && c == newCol)
                                {
                                    int oldRow = game.CurrentChecker.Row;
                                    int oldCol = game.CurrentChecker.Col;

                                    game.CurrentChecker.Row = newRow;
                                    game.CurrentChecker.Col = newCol;

                                    BacklightStroke(oldRow, oldCol, game.CurrentChecker, game);
                                    СhangePositionChecker(game, oldRow, oldCol);

                                    ChangeTurnPlayers(game);
                                }
                                else
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void IsSetChesckerKing(Game game)
        {
            if (game.CurrentChecker.Color == CheckerColor.White)
            {
                if (game.CurrentChecker.Row == 0)
                {
                    game.CurrentChecker.Type = CheckerType.King;

                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"change_checker_type(Row:{game.CurrentChecker.Row};Col:{game.CurrentChecker.Col};Color:{game.CurrentChecker.Color})$"), game.Player1);
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"change_checker_type(Row:{game.CurrentChecker.Row};Col:{game.CurrentChecker.Col};Color:{game.CurrentChecker.Color})$"), game.Player2);
                }
            }
            else if (game.CurrentChecker.Color == CheckerColor.Black)
            {
                if (game.CurrentChecker.Row == SizeBoard - 1)
                {
                    game.CurrentChecker.Type = CheckerType.King;

                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"change_checker_type(Row:{game.CurrentChecker.Row};Col:{game.CurrentChecker.Col};Color:{game.CurrentChecker.Color})$"), game.Player1);
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"change_checker_type(Row:{game.CurrentChecker.Row};Col:{game.CurrentChecker.Col};Color:{game.CurrentChecker.Color})$"), game.Player2);
                }
            }
        }

        private void GetSetAccount(Game game)
        {
            List<Checker> whiteCheckers = game.Checkers.Where(c => c.Color == CheckerColor.White).ToList();
            List<Checker> blackCheckers = game.Checkers.Where(c => c.Color == CheckerColor.Black).ToList();

            int white = whiteCheckers == null ? 0 : whiteCheckers.Count;
            int black = blackCheckers == null ? 0 : blackCheckers.Count;
            
            byte[] buffer = Encoding.GetEncoding(1251).GetBytes($"Account(White:{white};Black:{black})$");
            SendMessagePlayer(buffer, game.Player1);
            SendMessagePlayer(buffer, game.Player2);
            
            if (white == 0 || black == 0)
            {
                GameEnd(game, white, black);
            }
        }

        private void BacklightStroke(int oldRow, int oldCol, Checker c2, Game game)
        {
            string[] strokes = new[]
            {
                $"BacklightStroke(Row:{oldRow};Col:{oldCol};Clear:{1})$",
                $"BacklightStroke(Row:{c2.Row};Col:{c2.Col};Clear:{0})$"
            };

            foreach (string stroke in strokes)
            {
                byte[] buffer = Encoding.GetEncoding(1251).GetBytes(stroke);

                SendMessagePlayer(buffer, game.Player1);
                SendMessagePlayer(buffer, game.Player2);
            }
        }

        private void ChangeTurnPlayers(Game game)
        {
            game.Player1.YourTurn = !game.Player1.YourTurn;
            game.Player2.YourTurn = !game.Player2.YourTurn;

            SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"your_turn:{game.Player1.YourTurn}$"), game.Player1);
            SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes($"your_turn:{game.Player2.YourTurn}$"), game.Player2);

            if (game.Player1.YourTurn)
            {
                List<Checker> checkers = game.Checkers.Where(c => c.Color == CheckerColor.White).ToList();
                List<bool> results = new List<bool>();

                foreach (Checker checker in checkers)
                {
                    // возвращается true если ход возможен
                    results.Add(BacklightingStrokes(game, checker, game.Player1, TypeIllumination.Hide, true, false));
                }

                bool isEnd = true;

                foreach (bool result in results)
                {
                    if (result)
                    {
                        isEnd = false;
                        break;
                    }
                }

                if (isEnd)
                {
                    foreach (Checker checker in checkers)
                    {
                        game.Checkers.Remove(checker);
                    }

                    GameEnd(game, game.Checkers.Where(c => c.Color == CheckerColor.White).ToList().Count,
                        game.Checkers.Where(c => c.Color == CheckerColor.Black).ToList().Count);
                }
            }
            else if (game.Player2.YourTurn)
            {
                List<Checker> checkers = game.Checkers.Where(c => c.Color == CheckerColor.Black).ToList();
                List<bool> results = new List<bool>();

                foreach (Checker checker in checkers)
                {
                    // возвращается true если ход возможен
                    results.Add(BacklightingStrokes(game, checker, game.Player2, TypeIllumination.Hide, true, false));
                }

                bool isEnd = true;

                foreach (bool result in results)
                {
                    if (result)
                    {
                        isEnd = false;
                        break;
                    }
                }

                if (isEnd)
                {
                    foreach (Checker checker in checkers)
                    {
                        game.Checkers.Remove(checker);
                    }

                    GameEnd(game, game.Checkers.Where(c => c.Color == CheckerColor.White).ToList().Count,
                        game.Checkers.Where(c => c.Color == CheckerColor.Black).ToList().Count);
                }
            }
        }

        private void СhangePositionChecker(Game game, int oldRow, int oldCol)
        {
            byte[] buffer = Encoding.GetEncoding(1251).GetBytes($"Old[Row:{oldRow};Col:{oldCol}]New[Row:{game.CurrentChecker.Row};Col:{game.CurrentChecker.Col}]$");

            SendMessagePlayer(buffer, game.Player1);
            SendMessagePlayer(buffer, game.Player2);
        }

        private void RemoveChecker(Game game, int removeCheckerRow, int removeCheckerCol)
        {
            byte[] buffer = Encoding.GetEncoding(1251).GetBytes($"remove_checker(Row:{removeCheckerRow};Col:{removeCheckerCol})$");

            Checker checker = game.Checkers.FirstOrDefault(c => c.Row == removeCheckerRow && c.Col == removeCheckerCol);

            if (checker != null)
            {
                game.Checkers.Remove(checker);

                SendMessagePlayer(buffer, game.Player1);
                SendMessagePlayer(buffer, game.Player2);
            }

            GetSetAccount(game);
        }

        private void GameEnd(Game game, int white, int black)
        {
            try
            {
                game.Status = GameStatus.Finished;
                game.Player1.Status = Status.NotPlaying;
                game.Player2.Status = Status.NotPlaying;

                if (white == 0)
                {
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("GameEnd(win)$"), game.Player2);
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("GameEnd(loss)$"), game.Player1);
                }
                else if (black == 0)
                {
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("GameEnd(win)$"), game.Player1);
                    SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("GameEnd(loss)$"), game.Player2);
                }
            }
            catch 
            {
                // ignored
            }
        }

        private bool BacklightingStrokes(Game game, Checker checker, Player player, TypeIllumination type, bool isEnd, bool isBlock)
        {
            bool isPossibleMove = false;

            if (checker.Color != player.Color)
            {
                return false;
            }

            if (type == TypeIllumination.Full ||
                type == TypeIllumination.OnlyStroke)
            {
                game.CurrentChecker = checker;
            }

            // снизу в верх
            if (player.Color == CheckerColor.White)
            {
                // движение вверх на лево на 1
                int row = checker.Row - 1;
                int col = checker.Col - 1;

                if (row >= 0 && col >= 0)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 == null)
                    {
                        // draw
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, row, col);
                        }

                        if (isEnd)
                            isPossibleMove = true;
                    }
                    else
                    {
                        row = checker.Row - 2;
                        col = checker.Col - 2;

                        if (row >= 0 && col >= 0)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                }

                // движение вверх на право на 1
                row = checker.Row - 1;
                col = checker.Col + 1;

                if (row >= 0 && col < SizeBoard)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 == null)
                    {
                        // draw
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, row, col);
                        }

                        if (isEnd)
                            isPossibleMove = true;
                    }
                    else
                    {
                        row = checker.Row - 2;
                        col = checker.Col + 2;

                        if (row >= 0 && col < SizeBoard)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                }

                // вниз на лево
                row = checker.Row + 1;
                col = checker.Col - 1;

                if (row < SizeBoard && col >= 0)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 != null)
                    {
                        row = checker.Row + 2;
                        col = checker.Col - 2;

                        if (row < SizeBoard && col >= 0)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (checker.Type == CheckerType.King)
                        {
                            if (type == TypeIllumination.Full)
                            {
                                IlluminationStroke(player.Socket, row, col);
                            }
                        }
                    }
                }

                // вниз на право
                row = checker.Row + 1;
                col = checker.Col + 1;

                if (row < SizeBoard && col < SizeBoard)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 != null)
                    {
                        row = checker.Row + 2;
                        col = checker.Col + 2;

                        if (row < SizeBoard && col < SizeBoard)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (checker.Type == CheckerType.King)
                        {
                            if (type == TypeIllumination.Full)
                            {
                                IlluminationStroke(player.Socket, row, col);
                            }
                        }
                    }
                }
            }
            // сверху вниз
            else
            {
                int row = checker.Row + 1;
                int col = checker.Col - 1;

                // движение вниз на лево
                if (row < SizeBoard && col >= 0)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 == null)
                    {
                        // draw
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, row, col);
                        }

                        if (isEnd)
                            isPossibleMove = true;
                    }
                    else
                    {
                        row = checker.Row + 2;
                        col = checker.Col - 2;

                        if (row < SizeBoard && col >= 0)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                }

                // движение вниз на право
                row = checker.Row + 1;
                col = checker.Col + 1;

                if (row < SizeBoard && col < SizeBoard)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 == null)
                    {
                        // draw
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, row, col);
                        }

                        if (isEnd)
                            isPossibleMove = true;
                    }
                    else
                    {
                        row = checker.Row + 2;
                        col = checker.Col + 2;

                        if (row < SizeBoard && col < SizeBoard)
                        {
                            if (c1.Color != player.Color)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                }

                // вверх на лево
                row = checker.Row - 1;
                col = checker.Col - 1;

                if (row >= 0 && col >= 0)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 != null)
                    {
                        if (c1.Color != player.Color)
                        {
                            row = checker.Row - 2;
                            col = checker.Col - 2;

                            if (row >= 0 && col >= 0)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (checker.Type == CheckerType.King)
                        {
                            if (type == TypeIllumination.Full)
                            {
                                IlluminationStroke(player.Socket, row, col);
                            }
                        }
                    }
                }

                // вверх на право
                row = checker.Row - 1;
                col = checker.Col + 1;

                if (row >= 0 && col < SizeBoard)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                    if (c1 != null)
                    {
                        if (c1.Color != player.Color)
                        {
                            row = checker.Row - 2;
                            col = checker.Col + 2;

                            if (row >= 0 && col < SizeBoard)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(c => c.Row == row && c.Col == col);

                                if (c2 == null)
                                {
                                    // draw
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, row, col);
                                    }

                                    isPossibleMove = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (checker.Type == CheckerType.King)
                        {
                            if (type == TypeIllumination.Full)
                            {
                                IlluminationStroke(player.Socket, row, col);
                            }
                        }
                    }
                }
            }

            // если дамка
            if (checker.Type == CheckerType.King)
            {
                // диагональ вверх на лево
                for (int r = checker.Row - 1, c = checker.Col - 1; r >= 0 && c >= 0; --r, --c)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                    if (c1 == null)
                    {
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, r, c);
                            
                            isPossibleMove = true;
                        }
                    }
                    else
                    {
                        if (c1.Color != player.Color)
                        {
                            int newRow = --r;
                            int newCol = --c;

                            if (newRow >= 0 && newCol >= 0)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == newRow && ch.Col == newCol);

                                if (c2 == null)
                                {
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, r, c);
                                        isPossibleMove = true;

                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // диагональ вверх на право
                for (int r = checker.Row - 1, c = checker.Col + 1; r >= 0 && c < SizeBoard; --r, ++c)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                    if (c1 == null)
                    {
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, r, c);
                            isPossibleMove = true;
                        }
                    }
                    else
                    {
                        if (c1.Color != player.Color)
                        {
                            int newRow = --r;
                            int newCol = ++c;

                            if (newRow >= 0 && newCol < SizeBoard)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == newRow && ch.Col == newCol);

                                if (c2 == null)
                                {
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, r, c);
                                        isPossibleMove = true;

                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // диагональ вниз на лево
                for (int r = checker.Row + 1, c = checker.Col - 1; r < SizeBoard && c >= 0; ++r, --c)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                    if (c1 == null)
                    {
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, r, c);
                            isPossibleMove = true;
                        }
                    }
                    else
                    {
                        if (c1.Color != player.Color)
                        {
                            int newRow = ++r;
                            int newCol = --c;

                            if (newRow < SizeBoard && newCol >= 0)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == newRow && ch.Col == newCol);

                                if (c2 == null)
                                {
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, r, c);
                                        isPossibleMove = true;

                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // диагональ вниз на право
                for (int r = checker.Row + 1, c = checker.Col + 1; r < SizeBoard && c < SizeBoard; ++r, ++c)
                {
                    Checker c1 = game.Checkers.FirstOrDefault(ch => ch.Row == r && ch.Col == c);

                    if (c1 == null)
                    {
                        if (type == TypeIllumination.Full)
                        {
                            IlluminationStroke(player.Socket, r, c);
                            isPossibleMove = true;
                        }
                    }
                    else
                    {
                        if (c1.Color != player.Color)
                        {
                            int newRow = ++r;
                            int newCol = ++c;

                            if (newRow < SizeBoard && newCol < SizeBoard)
                            {
                                Checker c2 = game.Checkers.FirstOrDefault(ch => ch.Row == newRow && ch.Col == newCol);

                                if (c2 == null)
                                {
                                    if (type == TypeIllumination.Full ||
                                        type == TypeIllumination.OnlyStroke)
                                    {
                                        IlluminationStroke(player.Socket, r, c);
                                        isPossibleMove = true;

                                        break;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            if (isPossibleMove && isBlock &&
                (type == TypeIllumination.Full || type == TypeIllumination.OnlyStroke))
            {
                SendMessagePlayer(
                    Encoding.GetEncoding(1251).GetBytes($"block(Row:{game.CurrentChecker.Col};Col:{game.CurrentChecker.Col};Color:{game.CurrentChecker.Color})$"),
                    player);
            }
            else
            {
                SendMessagePlayer(Encoding.GetEncoding(1251).GetBytes("clear_blocking$"), player);
            }

            return isPossibleMove;
        }

        private async void IlluminationStroke(Socket playerSocket, int row, int col)
        {
            try
            {
                byte[] buffer = Encoding.GetEncoding(1251).GetBytes($"IlluminationStroke(Row:{row};Col:{col})$");

                await Task.Factory.FromAsync(playerSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                    res => playerSocket.EndSend(res));
            }
            catch (Exception)
            {

            }
        }

        public void StopServer()
        {
            _tockenSource.Cancel();
        }
    }
}