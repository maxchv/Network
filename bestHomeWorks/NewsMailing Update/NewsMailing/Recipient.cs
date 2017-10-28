namespace NewsMailing
{
    public class Recipient
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string Surname { get; set; }

        public string Email { get; set; }

        public override string ToString()
        {
            return FirstName + " " + Surname;
        }
    }
}
