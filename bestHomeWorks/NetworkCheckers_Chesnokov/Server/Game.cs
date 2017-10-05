using System;
using System.Collections.Generic;

namespace Server
{
    enum GameStatus
    {
        Goes,
        Finished,
        WaitingOpponent
    }

    class Game
    {
        public Player Player1 { get; set; }

        public Player Player2 { get; set; }

        public DateTime StartGame { get; set; }

        public DateTime EndGame { get; set; }

        public GameStatus Status { get; set; }

        public List<Checker> Checkers { get; set; }

        private const int _sizeBoard = 8;

        public Checker CurrentChecker { get; set; }

        public Game()
        {
            Checkers = new List<Checker>();

            bool isEven = false;
            for (int row = 0; row < _sizeBoard; row++)
            {
                for (int col = 0; col < _sizeBoard; col++)
                {
                    if ((row == 0 ||
                         row == 1 ||
                         row == 2) && isEven)
                    {
                        Checker checker = new Checker();
                        checker.Row = row;
                        checker.Col = col;
                        checker.Color = CheckerColor.Black;
                        checker.Type = CheckerType.Simple;

                        Checkers.Add(checker);
                    }
                    else if ((row == _sizeBoard - 1 ||
                             row == _sizeBoard - 2 ||
                             row == _sizeBoard - 3) && isEven)
                    {
                        Checker checker = new Checker();
                        checker.Row = row;
                        checker.Col = col;
                        checker.Color = CheckerColor.White;
                        checker.Type = CheckerType.Simple;

                        Checkers.Add(checker);
                    }

                    isEven = !isEven;
                }
                isEven = !isEven;
            }
            //isEven = !isEven;
        }
    }
}