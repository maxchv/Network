using System.Windows.Controls;
using System.Windows.Media;

namespace Client
{
    enum CheckerColor
    {
        Black,
        White
    }

    class BoardCell
    {
        public Button Button { get; set; }

        public int Row { get; set; }

        public int Col { get; set; }

        public Brush Background { get; set; }

        //public CheckerColor Type { get; set; }
    }
}