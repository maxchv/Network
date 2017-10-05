namespace Server
{
    enum CheckerColor
    {
        Black,
        White
    }

    enum CheckerType
    {
        Simple,
        King
    }

    class Checker
    {
        public int Row { get; set; }

        public int Col { get; set; }

        public CheckerColor Color { get; set; }

        public CheckerType Type { get; set; }

        public override string ToString()
        {
            return $"Checker(Row:{Row};Col:{Col};Color:{Color})$";
        }
    }
}