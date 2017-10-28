using System.Text.RegularExpressions;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;

namespace NewsMailing
{
    /// <summary>
    /// Interaction logic for AddNewRecipientWindow.xaml
    /// </summary>
    public partial class AddNewRecipientWindow
    {
        public Recipient Recipient { get; set; }

        public AddNewRecipientWindow()
        {
            InitializeComponent();
        }

        private async void AddNewRecipientButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Surname.Text) ||
                string.IsNullOrWhiteSpace(Surname.Text))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите фамилию!");
                return;
            }

            if (string.IsNullOrEmpty(FirstName.Text) ||
                string.IsNullOrWhiteSpace(FirstName.Text))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите имя!");
                return;
            }

            if (!Regex.IsMatch(Email.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$"))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите валидный email!");
                return;
            }
            
            Recipient newRecipient = new Recipient();
            newRecipient.Surname = Surname.Text;
            newRecipient.FirstName = FirstName.Text;
            newRecipient.Email = Email.Text;

            Recipient = newRecipient;

            DialogResult = true;
        }
    }
}