using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MailAgent
{
    public partial class Form1 : Form
    {
        public string FileName { get; private set; }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try {
                SmtpClient smtp = new SmtpClient(tbSmtp.Text, Convert.ToInt32(tbPort.Text));
                smtp.Credentials = new NetworkCredential(tbEmail.Text, tbPassw.Text);
                smtp.EnableSsl = true;

                MailMessage mail = new MailMessage();
                mail.Body = tbMsg.Text;
                mail.IsBodyHtml = true;
                mail.Subject = tbSubject.Text;
                mail.From = new MailAddress(tbEmail.Text, "Test Agent");
                //foreach(string to in tbRecepient.Text.Split(';'))
                {
                    //mail.To.Add(new MailAddress(to.Trim()));
                    mail.To.Add(new MailAddress(tbRecepient.Text));
                }

                if(!string.IsNullOrEmpty(FileName))
                {
                    Attachment attach = new Attachment(FileName);
                    mail.Attachments.Add(attach);
                }

                smtp.Send(mail);
                statusLabel.Text = "Sended successfully";
            }
            catch (Exception ex)
            {
                statusLabel.Text = ex.Message;
            }
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Multiselect = false;
            if(dlg.ShowDialog() == DialogResult.OK)
            {
                FileName = dlg.FileName;
            }
        }
    }
}
