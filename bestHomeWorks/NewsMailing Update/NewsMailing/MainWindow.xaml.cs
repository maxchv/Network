using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace NewsMailing
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private ObservableCollection<Recipient> _recipients;
        private ObservableCollection<AttachedFile> _attachedFiles;
        private Random _random;
        private bool _isCanceled;

        public MainWindow()
        {
            InitializeComponent();

            _recipients = new ObservableCollection<Recipient>();
            _attachedFiles = new ObservableCollection<AttachedFile>();
            _random = new Random();

            ReceiversList.ItemsSource = _recipients;
            AttachedFilesListView.ItemsSource = _attachedFiles;
            _isCanceled = false;
        }

        private void RemoveRecipientBadged_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Badged badged = sender as Badged;

                if (badged != null)
                {
                    Recipient recipient = _recipients.FirstOrDefault(t => t.Id == Convert.ToInt32(badged.Tag.ToString()));

                    if (recipient != null)
                    {
                        _recipients.Remove(recipient);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void ClearReceiversListButton_OnClick(object sender, RoutedEventArgs e)
        {
            _recipients.Clear();
        }

        private void LoadReceiversFromFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "CSV UTF-8 (разделитель - запятая) (*.csv)|*.csv";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                ParseFile(dialog.FileName);
            }
        }

        private async void ParseFile(string filePath)
        {
            try
            {
                List<string> receiverList = new List<string>();
                string tempLine;

                using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251)))
                {
                    do
                    {
                        tempLine = await reader.ReadLineAsync();

                        if (tempLine != null)
                            receiverList.Add(tempLine);

                    } while (tempLine != null);
                }

                foreach (string line in receiverList)
                {
                    string[] recipientParameters = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    if (recipientParameters.Length == 3)
                    {
                        Recipient newRecipient = new Recipient();
                        newRecipient.Id = GetRecipientId();
                        newRecipient.Surname = recipientParameters[0];
                        newRecipient.FirstName = recipientParameters[1];
                        newRecipient.Email = recipientParameters[2];

                        _recipients.Add(newRecipient);
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetRecipientId()
        {
            int id = 1;

            foreach (var recipient in _recipients)
            {
                if (recipient.Id == id)
                {
                    id++;
                }
            }

            return id;
        }

        private int GetAttachedFileId()
        {
            int id = 1;

            foreach (var file in _attachedFiles)
            {
                if (file.Id == id)
                {
                    id++;
                }
            }

            return id;
        }

        private void AddNewRecipientButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                AddNewRecipientWindow window = new AddNewRecipientWindow();

                if (window.ShowDialog() == true)
                {
                    Recipient newRecipient = window.Recipient;
                    newRecipient.Id = GetRecipientId();

                    _recipients.Add(newRecipient);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveAttachedFileBadged_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Badged badged = sender as Badged;

                if (badged != null)
                {
                    AttachedFile file = _attachedFiles.FirstOrDefault(f => f.Id == Convert.ToInt32(badged.Tag.ToString()));

                    if (file != null)
                    {
                        _attachedFiles.Remove(file);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private void LoadAttachedFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            dialog.Filter = "Все файлы (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    AttachedFile newAttachedFile = new AttachedFile();
                    newAttachedFile.Id = GetAttachedFileId();
                    newAttachedFile.Name = Path.GetFileName(fileName);
                    newAttachedFile.Path = fileName;

                    _attachedFiles.Add(newAttachedFile);
                }
            }
        }

        private void ClearMessageButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageTextbox.Text = String.Empty;
        }

        private void IsBodyHtmlCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBox != null)
            {
                IsFileBodyHtmlCheckBox.Visibility = (checkBox.IsChecked == true) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void IsFileBodyHtmlCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBox != null)
            {
                HtmlFileGrid.Visibility = (checkBox.IsChecked == true) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void BrowseHtmlFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = "HTML (*.html)|*.html";

            if (dialog.ShowDialog() == true)
            {
                HtmlFileBodyMessageTextBlock.Text = dialog.FileName;
            }
        }

        private async void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(EmailSender.Text) ||
                string.IsNullOrWhiteSpace(EmailSender.Text))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите Email отправителя!");
                return;
            }

            if (!Regex.IsMatch(EmailSender.Text, @"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,5})+)$"))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите валидный Email отправителя!");
                return;
            }

            if (string.IsNullOrEmpty(PasswordEmailSender.Password) ||
                string.IsNullOrWhiteSpace(PasswordEmailSender.Password))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите пароль от почты отпровителя!");
                return;
            }

            if (string.IsNullOrEmpty(SubjectTextBox.Text) ||
                string.IsNullOrWhiteSpace(SubjectTextBox.Text))
            {
                await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите тему письма!");
                return;
            }

            string body = string.Empty;
            string subject = SubjectTextBox.Text;
            if (IsBodyHtmlCheckBox.IsChecked == false)
            {
                if (string.IsNullOrEmpty(MessageTextbox.Text) ||
                    string.IsNullOrWhiteSpace(MessageTextbox.Text))
                {
                    await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите текст письма!");
                    return;
                }

                body = MessageTextbox.Text;
            }
            else if (IsBodyHtmlCheckBox.IsChecked == true)
            {
                if (IsFileBodyHtmlCheckBox.IsChecked == false)
                {
                    if (string.IsNullOrEmpty(MessageTextbox.Text) ||
                        string.IsNullOrWhiteSpace(MessageTextbox.Text))
                    {
                        await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите HTML код!");
                        return;
                    }

                    body = MessageTextbox.Text;
                }
                else if (IsFileBodyHtmlCheckBox.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(HtmlFileBodyMessageTextBlock.Text) ||
                        string.IsNullOrWhiteSpace(HtmlFileBodyMessageTextBlock.Text))
                    {
                        await this.ShowMessageAsync("Ошибка входных данных", "Пожалуйста укажите путь к файлу из которого будет формироваться сообщение!");
                        return;
                    }

                    if (!File.Exists(HtmlFileBodyMessageTextBlock.Text))
                    {
                        await this.ShowMessageAsync("Ошибка входных данных", "Указанный файл не был найден!");
                        return;
                    }

                    body = File.ReadAllText(HtmlFileBodyMessageTextBlock.Text);
                }
            }

            ProgressGrid.Visibility = Visibility.Visible;
            ProgressInformationTextBlock.Text = "Пожалуйста подождите идет рассылка писем...";
            _isCanceled = false;
            try
            {
                MailAddress from = new MailAddress(EmailSender.Text, NicknameEmailSender.Text);

                for (int i = 0; i < _recipients.Count; i++)
                {
                    if (_isCanceled)
                        break;

                    MailAddress to = new MailAddress(_recipients[i].Email, _recipients[i].ToString());
                    SendMail(from, to, body, subject, PasswordEmailSender.Password);

                    if (_isCanceled || i == _recipients.Count - 1)
                        break;
                    
                    await Task.Delay(_random.Next(Convert.ToInt32(ConfigurationManager.AppSettings["time_minimum"]),
                        Convert.ToInt32(ConfigurationManager.AppSettings["time_maximum"])));
                }
                //foreach (var recipient in _recipients)
                //{
                //    if (_isCanceled)
                //        break;

                //    MailAddress to = new MailAddress(recipient.Email);
                //    SendMail(from, to, body, subject, PasswordEmailSender.Password);

                //    if (_isCanceled)
                //        break;

                //    await Task.Delay(_random.Next(Convert.ToInt32(ConfigurationManager.AppSettings["time_minimum"]),
                //        Convert.ToInt32(ConfigurationManager.AppSettings["time_maximum"])));
                //}
            }
            catch
            {
                // ignored
            }

            ProgressGrid.Visibility = Visibility.Hidden;
            MessageBox.Show("Массовая рассылка успешно законченна!", "Массовая рассылка",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void SendMail(MailAddress from, MailAddress to, string body, string subject, string password)
        {
            try
            {
                SmtpClient smtpClient = new SmtpClient(ConfigurationManager.AppSettings["host"],
                            Convert.ToInt32(ConfigurationManager.AppSettings["port"]));
                smtpClient.EnableSsl = true;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.Credentials = new NetworkCredential(from.Address, password);

                MailMessage mailMessage = new MailMessage(from, to);

                foreach (var file in _attachedFiles)
                {
                    if (File.Exists(file.Path))
                    {
                        Attachment attachment = new Attachment(file.Path);
                        mailMessage.Attachments.Add(attachment);
                    }
                }

                if (IsBodyHtmlCheckBox.IsChecked == false)
                {
                    mailMessage.IsBodyHtml = false;
                }
                else if (IsBodyHtmlCheckBox.IsChecked == true)
                {
                    mailMessage.IsBodyHtml = true;
                }

                mailMessage.Body = body;
                mailMessage.Subject = subject;

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelMailingButton_OnClick(object sender, RoutedEventArgs e)
        {
            _isCanceled = true;
            ProgressInformationTextBlock.Text = "Пожалуйста подождите идет остановка рассылки писем...";
        }
    }
}