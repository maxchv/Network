using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        private static object tbSmtp;

        static void Main(string[] args)
        {
            SmtpClient smtp = new SmtpClient("smtp.mail.ru", 587);
            smtp.Credentials = new NetworkCredential("mmylastname@mail.ua", "qwerty12345");
            smtp.EnableSsl = true;

            MailMessage mail = new MailMessage();
            mail.Body = "<h1>Hello</h1>";
            mail.IsBodyHtml = true;
            mail.Subject = "test";
            mail.From = new MailAddress("mmylastname@mail.ua", "Test Agent");
            mail.To.Add(new MailAddress("mmylastname@mail.ua", "test"));


            smtp.Send(mail);
        }
    }
}
