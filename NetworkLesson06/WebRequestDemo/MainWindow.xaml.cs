using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WebRequestDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            tbContent.Text = "";
            string sUrl = tbUrl.Text;
            if (!string.IsNullOrEmpty(sUrl))
            {
                // запрос
                HttpWebRequest req = WebRequest.Create(sUrl) as HttpWebRequest;
                NetworkCredential nc = new NetworkCredential("user", "12345");
                req.Credentials = nc;
                req.AllowAutoRedirect = true;
                req.MaximumAutomaticRedirections = 1;

                req.Method = "POST";
                req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                req.ContentType = "application/x-www-form-urlencoded";
                req.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.79 Safari/537.36";
                string postData = "user=user&pass=12345";
                byte[] postBytes = Encoding.ASCII.GetBytes(postData);
                req.ContentLength = postBytes.Length;
                using (Stream stream = req.GetRequestStream())
                {
                    stream.Write(postBytes, 0, postBytes.Length);
                }

                // ответ
                try
                {
                    HttpWebResponse resp = req.GetResponse() as HttpWebResponse;

                    foreach (var header in resp.Headers)
                    {
                        tbContent.Text += header + " " + resp.GetResponseHeader(header.ToString()) + "\n";
                    }

                    using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                    {
                        tbContent.Text += reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    tbContent.Text = ex.Message;
                }

            }
        }
    }
}
