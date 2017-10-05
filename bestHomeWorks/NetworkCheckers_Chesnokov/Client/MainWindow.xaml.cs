using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Socket _clientSocket;
        private readonly IPEndPoint _ipEndPoint;
        private CancellationTokenSource _tockenSource;
        private const int SizeBoard = 8;
        private int _sizeCell = 70;
        private List<BoardCell> _boardCells;
        private CheckerColor _color;
        private bool _isStroke;
        private bool _yourTurn;
        private Brush _backlightСolor;

        public MainWindow()
        {
            InitializeComponent();

            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _clientSocket.SendBufferSize = 100000;

            _ipEndPoint = new IPEndPoint(IPAddress.Parse(ConfigurationManager.AppSettings["ipAddress"]),
                                         Convert.ToInt32(ConfigurationManager.AppSettings["port"]));
            _tockenSource = new CancellationTokenSource();

            _boardCells = new List<BoardCell>();
            _isStroke = false;
            _backlightСolor = Brushes.YellowGreen;

            CreateBoard();
        }

        private void CreateBoard()
        {
            LettersOfChessboard1.RowDefinitions.Add(new RowDefinition());
            LettersOfChessboard2.RowDefinitions.Add(new RowDefinition());

            NumbersOfChessboard1.ColumnDefinitions.Add(new ColumnDefinition());
            NumbersOfChessboard2.ColumnDefinitions.Add(new ColumnDefinition());

            for (int i = 0; i < SizeBoard; i++)
            {
                Board.RowDefinitions.Add(new RowDefinition());
                Board.ColumnDefinitions.Add(new ColumnDefinition());

                LettersOfChessboard1.ColumnDefinitions.Add(new ColumnDefinition());
                LettersOfChessboard2.ColumnDefinitions.Add(new ColumnDefinition());

                NumbersOfChessboard1.RowDefinitions.Add(new RowDefinition());
                NumbersOfChessboard2.RowDefinitions.Add(new RowDefinition());
            }

            _sizeCell = Convert.ToInt32(Board.ActualHeight / SizeBoard);

            bool isEven = false;
            for (int row = 0; row < SizeBoard; row++)
            {
                for (int col = 0; col < SizeBoard; col++)
                {
                    if (!isEven)
                    {
                        Button button = new Button();
                        button.Width = button.Height = _sizeCell;
                        button.Background = Brushes.Bisque;
                        button.Foreground = Brushes.Transparent;
                        button.Style = (Style)FindResource("EnableButtonStyle");
                        button.Padding = new Thickness(0);

                        Grid.SetRow(button, row);
                        Grid.SetColumn(button, col);
                        Board.Children.Add(button);

                        BoardCell cell = new BoardCell();
                        cell.Button = button;
                        cell.Col = col;
                        cell.Row = row;
                        cell.Background = Brushes.Bisque;

                        _boardCells.Add(cell);

                        isEven = !isEven;
                    }
                    else
                    {
                        Button button = new Button();
                        button.Width = button.Height = _sizeCell;
                        button.Background = Brushes.Black;
                        button.Style = (Style)FindResource("ButtonStyle");
                        button.Padding = new Thickness(0);
                        button.Click += ButtonOnClick;

                        Grid.SetRow(button, row);
                        Grid.SetColumn(button, col);
                        Board.Children.Add(button);

                        BoardCell cell = new BoardCell();
                        cell.Button = button;
                        cell.Col = col;
                        cell.Row = row;
                        cell.Background = Brushes.Black;

                        _boardCells.Add(cell);

                        isEven = !isEven;
                    }
                }
                isEven = !isEven;
            }


            foreach (var grid in new [] { LettersOfChessboard1, LettersOfChessboard2 })
            {
                for (int col = 0, l = 'A'; col < SizeBoard; col++)
                {
                    Label block = new Label()
                    {
                        Content = Convert.ToChar(l++).ToString(),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Width = 30,
                        FontSize = 14,
                        FontFamily = new FontFamily("Segoe Script")
                    };

                    Grid.SetRow(block, 0);
                    Grid.SetColumn(block, col);

                    grid.Children.Add(block);
                }
            }

            foreach (var grid in new[] { NumbersOfChessboard1, NumbersOfChessboard2 })
            {
                for (int row = 0; row < SizeBoard; row++)
                {
                    Label block = new Label()
                    {
                        Content = (row + 1).ToString(),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Height = 30,
                        FontSize = 14,
                        FontFamily = new FontFamily("Segoe Script")
                    };

                    Grid.SetRow(block, row);
                    Grid.SetColumn(block, 0);

                    grid.Children.Add(block);
                }
            }
        }

        private void ButtonOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            Button button = sender as Button;

            BoardCell cell = _boardCells.FirstOrDefault(c => Equals(c.Button, button));

            if (cell != null && _yourTurn)
            {
                Image image = button.Content as Image;

                if (image != null)
                {
                    BitmapImage bitmapImage = image.Source as BitmapImage;

                    if (bitmapImage != null)
                    {
                        bool yourOwnChecker = false;
                        if ((bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/WhiteChecker.png")) ||
                             bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/WhiteCrown.png"))) &&
                            _color == CheckerColor.White)
                        {
                            yourOwnChecker = true;
                        }
                        else if ((bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BlackChecker.png")) ||
                                  bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BlackCrown.png"))) &&
                                 _color == CheckerColor.Black)
                        {
                            yourOwnChecker = true;
                        }

                        if (yourOwnChecker)
                        {
                            ClearIllumination();

                            string data = $"stroke_checker(Row:{cell.Row};Col:{cell.Col})$";
                            SendData(Encoding.GetEncoding(1251).GetBytes(data));
                        }
                    }
                }
                else
                {
                    if (Equals(cell.Button.Background, _backlightСolor))
                    {
                        ClearIllumination();

                        string data = $"movement_checker(Row:{cell.Row};Col:{cell.Col})$";
                        SendData(Encoding.GetEncoding(1251).GetBytes(data));
                    }
                }
            }
        }

        private void ConnectedToServerMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            if (item != null)
            {
                ConnectedToServer(item);
            }
        }

        private async void ConnectedToServer(MenuItem item)
        {
            try
            {
                Status.Text = "Ожидание соединения с сервером";

                await Task.Factory.FromAsync(_clientSocket.BeginConnect, _clientSocket.EndConnect, _ipEndPoint, null);

                if (_clientSocket.Connected)
                {
                    Status.Text = $"Подключение к серверу {_clientSocket.RemoteEndPoint} произошло успешно";

                    string result = String.Empty;

                    do
                    {
                        result = await this.ShowInputAsync("Имя пользователя", "Введите имя пользователя:");
                    } while (string.IsNullOrEmpty(result));

                    SendData(Encoding.GetEncoding(1251).GetBytes($"set_name:{result}$"));

                    ConversationStart();

                    item.IsEnabled = false;
                }
            }
            catch (Exception e)
            {
                Status.Text = e.Message;
            }
        }

        private async void ConversationStart()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int readBytes;
                string message;

                while (!_tockenSource.Token.IsCancellationRequested)
                {
                    readBytes = await Task.Factory.FromAsync(_clientSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, null, null),
                                                                result => _clientSocket.EndReceive(result));

                    message = Encoding.GetEncoding(1251).GetString(buffer, 0, readBytes);
                    string[] arr = message.Split('$');

                    foreach (string msg in arr)
                    {
                        //Log.Text += $"{msg}\r\n";
                        if (Regex.IsMatch(msg, @"info:(?<InfoMessage>.*)"))
                        {
                            Status.Text = Regex.Match(msg, @"info:(?<InfoMessage>.*)").Groups["InfoMessage"].Value;
                        }
                        else if (Regex.IsMatch(msg,
                            @"Checker\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)"))
                        {
                            StartGameMenuItem.Header = "Отмена";

                            Match match = Regex.Match(msg,
                                @"Checker\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            BoardCell boardCell = _boardCells.FirstOrDefault(b => b.Row == row &&
                                                                                  b.Col == col);

                            if (boardCell != null)
                            {
                                Uri uri = new Uri(
                                    match.Groups["Color"].Value.Contains("Black")
                                        ? "pack://application:,,,/Images/BlackChecker.png"
                                        : "pack://application:,,,/Images/WhiteChecker.png");

                                boardCell.Button.Content = new Image()
                                {
                                    Source = new BitmapImage(uri),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Width = boardCell.Button.ActualWidth - 5,
                                    Height = boardCell.Button.ActualHeight - 5,
                                    Margin = new Thickness(0),
                                    Stretch = Stretch.Uniform
                                };
                            }
                        }
                        else if (Regex.IsMatch(msg, @"set_checker_color:(?<CheckerColor>.*)"))
                        {
                            string type = Regex.Match(msg, @"set_checker_color:(?<CheckerColor>.*)")
                                .Groups["CheckerColor"]
                                .Value;

                            _color = type.Contains("Black") ? CheckerColor.Black : CheckerColor.White;
                        }
                        else if (Regex.IsMatch(msg, @"your_turn:(?<Turn>.*)"))
                        {
                            _yourTurn = Regex.Match(msg, @"your_turn:(?<Turn>.*)").Groups["Turn"].Value.Contains("True");
                            Title = _yourTurn ? "Ваш ход" : "Ход соперника";
                        }
                        else if (Regex.IsMatch(msg, @"IlluminationStroke\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"IlluminationStroke\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            BoardCell cell = _boardCells.FirstOrDefault(c => c.Row == row && c.Col == col);

                            if (cell != null)
                            {
                                cell.Button.Background = _backlightСolor;
                            }
                        }
                        else if (Regex.IsMatch(msg, @"Old\[Row:(?<OldRow>\d*);Col:(?<OldCol>\d*)\]New\[Row:(?<NewRow>\d*);Col:(?<NewCol>\d*)\]"))
                        {
                            Match match = Regex.Match(msg, @"Old\[Row:(?<OldRow>\d*);Col:(?<OldCol>\d*)\]New\[Row:(?<NewRow>\d*);Col:(?<NewCol>\d*)\]");

                            int oldRow = Convert.ToInt32(match.Groups["OldRow"].Value);
                            int oldCol = Convert.ToInt32(match.Groups["OldCol"].Value);

                            BoardCell c1 = _boardCells.FirstOrDefault(c => c.Row == oldRow && c.Col == oldCol);

                            if (c1 != null)
                            {
                                int newRow = Convert.ToInt32(match.Groups["NewRow"].Value);
                                int newCol = Convert.ToInt32(match.Groups["NewCol"].Value);

                                BoardCell c2 = _boardCells.FirstOrDefault(c => c.Row == newRow && c.Col == newCol);

                                if (c2 != null)
                                {
                                    c2.Button.Content = c1.Button.Content;
                                    c1.Button.Content = null;
                                }
                            }
                        }
                        else if (Regex.IsMatch(msg, @"remove_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"remove_checker\(Row:(?<Row>\d*);Col:(?<Col>\d*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            BoardCell cell = _boardCells.FirstOrDefault(c => c.Row == row && c.Col == col);

                            if (cell != null)
                            {
                                cell.Button.Content = null;
                            }
                        }
                        else if (Regex.IsMatch(msg, @"BacklightStroke\(Row:(?<Row>\d*);Col:(?<Col>\d*);Clear:(?<Clear>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"BacklightStroke\(Row:(?<Row>\d*);Col:(?<Col>\d*);Clear:(?<Clear>\d*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);
                            int clear = Convert.ToInt32(match.Groups["Clear"].Value);

                            BoardCell cell = _boardCells.FirstOrDefault(c => c.Row == row && c.Col == col);

                            if (cell != null)
                            {
                                if (clear == 1)
                                {
                                    ClearBacklightStroke();
                                }

                                cell.Button.BorderBrush = Brushes.Red;
                                cell.Button.BorderThickness = new Thickness(3);
                            }
                        }
                        else if (Regex.IsMatch(msg, @"Account\(White:(?<White>\d*);Black:(?<Black>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"Account\(White:(?<White>\d*);Black:(?<Black>\d*)\)");

                            int white = Convert.ToInt32(match.Groups["White"].Value);
                            int black = Convert.ToInt32(match.Groups["Black"].Value);

                            YourCheckerCount.Content = white.ToString();
                            OponentCheckerCount.Content = black.ToString();
                        }
                        else if (Regex.IsMatch(msg, @"set_oponent_nickname\((?<NickName>.*);Type:(?<Type>\d*)\)"))
                        {
                            Match match = Regex.Match(msg, @"set_oponent_nickname\((?<NickName>.*);Type:(?<Type>\d*)\)");

                            int type = Convert.ToInt32(match.Groups["Type"].Value);

                            if (type == 0)
                            {
                                YourNickName.Content = "Пользователь: " + match.Groups["NickName"].Value;
                            }
                            else
                            {
                                NickNameOponent.Content = "Пользователь: " + match.Groups["NickName"].Value;
                            }
                        }
                        else if (Regex.IsMatch(msg, @"GameEnd\((?<Result>.*)\)"))
                        {
                            string result = Regex.Match(msg, @"GameEnd\((?<Result>.*)\)").Groups["Result"].Value;

                            if (result.Equals("win"))
                            {
                                await this.ShowMessageAsync("Результат партии", "Поздравляем Вы выйграли!");
                            }
                            else if (result.Equals("loss"))
                            {
                                await this.ShowMessageAsync("Результат партии", "Увы Вы проиграли!");
                            }

                            GameOver();
                        }
                        else if (Regex.IsMatch(msg, @"change_checker_type\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)"))
                        {
                            Match match = Regex.Match(msg, @"change_checker_type\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);


                            BoardCell cell = _boardCells.FirstOrDefault(c => c.Row == row && c.Col == col);

                            if (cell != null)
                            {
                                Uri uri = new Uri(
                                    match.Groups["Color"].Value.Contains("Black")
                                        ? "pack://application:,,,/Images/BlackCrown.png"
                                        : "pack://application:,,,/Images/WhiteCrown.png");
                                
                                cell.Button.Content = new Image()
                                {
                                    Source = new BitmapImage(uri),
                                    VerticalAlignment = VerticalAlignment.Center,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Width = cell.Button.ActualWidth - 5,
                                    Height = cell.Button.ActualHeight - 5,
                                    Margin = new Thickness(0),
                                    Stretch = Stretch.Uniform
                                };
                            }
                        }
                        else if (Regex.IsMatch(msg, @"block\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)"))
                        {
                            Match match = Regex.Match(msg, @"block\(Row:(?<Row>\d*);Col:(?<Col>\d*);Color:(?<Color>.*)\)");

                            int row = Convert.ToInt32(match.Groups["Row"].Value);
                            int col = Convert.ToInt32(match.Groups["Col"].Value);

                            BoardCell cell = _boardCells.FirstOrDefault(c => c.Row == row && c.Col == col);

                            if (cell != null)
                            {
                                List<BoardCell> cells = _boardCells.Where(b => b.Button.Content != null).ToList();

                                if (cells != null)
                                {
                                    List<BoardCell> blockCells = new List<BoardCell>();
                                    foreach (BoardCell boardCell in cells)
                                    {
                                        Image image = boardCell.Button.Content as Image;

                                        if (image != null)
                                        {
                                            BitmapImage bitmapImage = image.Source as BitmapImage;

                                            if (bitmapImage != null)
                                            {
                                                if (match.Groups["Color"].Value.Contains("Black"))
                                                {
                                                    if((bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BlackCrown.png")) ||
                                                        bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BlackChecker.png"))) 
                                                        && boardCell != cell)
                                                    {
                                                        blockCells.Add(boardCell);
                                                    }
                                                }
                                                else if (match.Groups["Color"].Value.Contains("White"))
                                                {
                                                    if ((bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/WhiteCrown.png")) ||
                                                         bitmapImage.UriSource.Equals(new Uri("pack://application:,,,/Images/WhiteChecker.png")))
                                                        && boardCell != cell)
                                                    {
                                                        blockCells.Add(boardCell);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    foreach (BoardCell boardCell in blockCells)
                                    {
                                        boardCell.Button.IsEnabled = false;
                                    }
                                }
                            }
                        }
                        else if (msg.Contains("clear_blocking"))
                        {
                            foreach (BoardCell cell in _boardCells)
                            {
                                if (cell.Background != Brushes.Bisque)
                                {
                                    cell.Button.IsEnabled = true;
                                }
                            }
                        }
                        //else if (msg.Contains("ClearBacklightStroke"))
                        //{
                        //    ClearBacklightStroke();
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                Status.Text = e.Message;
            }
        }

        private void GameOver()
        {
            ClearBacklightStroke();
            ClearIllumination();
            StartGameMenuItem.IsEnabled = true;
            
            foreach (BoardCell cell in _boardCells)
            {
                cell.Button.Content = null;
            }
        }

        private void StartGameMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (StartGameMenuItem.Header.Equals("Начать игру"))
            {
                if (_clientSocket != null)
                {
                    if (_clientSocket.Connected)
                    {

                        SendData(Encoding.GetEncoding(1251).GetBytes("Start_Game$"));

                        StartGameMenuItem.IsEnabled = true;
                    }
                }
            }
            else if(StartGameMenuItem.Header.Equals("Отмена"))
            {
                SendData(Encoding.GetEncoding(1251).GetBytes("i_give_up"));

                StartGameMenuItem.Header = "Начать игру";
            }
        }

        private async void SendData(byte[] data)
        {
            try
            {
                if (_clientSocket != null)
                {
                    await Task.Factory.FromAsync(_clientSocket.BeginSend(data, 0, data.Length, SocketFlags.None, null, null),
                                                res => _clientSocket.EndSend(res));
                }
            }
            catch (Exception e)
            {
                Status.Text = e.Message;
            }
        }

        private void Board_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _sizeCell = Convert.ToInt32(BoardBorder.ActualHeight / SizeBoard);

            foreach (var cell in _boardCells)
            {
                cell.Button.Height = cell.Button.Width = _sizeCell;

                Image image = cell.Button.Content as Image;

                if (image != null)
                {
                    image.Height = cell.Button.Height - 5;
                    image.Width = cell.Button.Width - 5;
                    cell.Button.Content = image;
                }
            }

            foreach (var grid in new [] {LettersOfChessboard1, LettersOfChessboard2})
            {
                foreach (var child in grid.Children)
                {
                    Label label = child as Label;

                    if (label != null)
                    {
                        label.Width = _sizeCell;
                    }
                }
            }

            foreach (var grid in new[] { NumbersOfChessboard1, NumbersOfChessboard2 })
            {
                foreach (var child in grid.Children)
                {
                    Label label = child as Label;

                    if (label != null)
                    {
                        label.Height = _sizeCell;
                    }
                }
            }
        }

        private void ClearIllumination()
        {
            foreach (BoardCell cell in _boardCells)
            {
                if (cell.Background != Brushes.Bisque)
                {
                    cell.Button.Background = Brushes.Black;
                }
            }
        }

        private void ClearBacklightStroke()
        {
            List<BoardCell> cells = _boardCells.Where(c => Equals(c.Button.BorderBrush, Brushes.Red) &&
                                        c.Button.BorderThickness == new Thickness(3)).ToList();

            foreach (BoardCell cell in cells)
            {
                cell.Button.BorderBrush = Brushes.Black;
                cell.Button.BorderThickness = new Thickness(1);
            }
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (StartGameMenuItem.Header.Equals("Отмена"))
            {
                SendData(Encoding.GetEncoding(1251).GetBytes("i_give_up"));
            }
        }
    }
}