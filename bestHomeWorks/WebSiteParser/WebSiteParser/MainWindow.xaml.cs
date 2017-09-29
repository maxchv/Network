using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using HtmlAgilityPack;

namespace WebSiteParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private object _loker = new object();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ParseButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(RootFolderTextBox.Text))
                {
                    MessageBox.Show("Укажите путь куда будет загружен сайт!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(UrlTextBox.Text))
                {
                    MessageBox.Show("Укажите URL загружаемого сайта!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Directory.Exists(RootFolderTextBox.Text))
                {
                    MessageBox.Show("Указанный путь неверный!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Uri uri = new Uri(UrlTextBox.Text);

                DirectoryInfo directory = new DirectoryInfo(RootFolderTextBox.Text);
                directory = directory.CreateSubdirectory(uri.Host);
                Directory.CreateDirectory(directory.FullName);

                string domain = UrlTextBox.Text[UrlTextBox.Text.Length - 1].Equals('/') ?
                                                UrlTextBox.Text : UrlTextBox.Text + "/";

                MetroProgressBar.Visibility = Visibility.Visible;
                BrowseFolderButton.IsEnabled = false;
                ParseSiteButton.IsEnabled = false;

                Parse(domain, domain, directory);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Parse(string url, string domain, DirectoryInfo directory)
        {
            ThreadPool.QueueUserWorkItem((arg) =>
            {
                string currentUrl = url.IndexOf(domain) != -1 ? domain : domain + url;
                DirectoryInfo currentDirectory = directory;
                string filePath = GetProspectivePath(url, currentUrl, currentDirectory);

                if (File.Exists(filePath))
                {
                    return;
                }

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(currentUrl);
                    request.Method = "GET";
                    request.KeepAlive = true;
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/61.0.3163.91 Safari/537.36";

                    WebResponse response = request.GetResponse();

                    StringBuilder responseBuilder = new StringBuilder();
                    byte[] buffer = new byte[5000];
                    int bytesRead;

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        string ext = Path.GetExtension(filePath);
                        using (BinaryReader br = new BinaryReader(responseStream))
                        using (FileStream fs = new FileStream(filePath, FileMode.Create))
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            while ((bytesRead = br.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                bw.Write(buffer, 0, bytesRead);
                                responseBuilder.Append(Encoding.GetEncoding(1251).GetString(buffer, 0, bytesRead));
                            }
                        }

                        if (!ext.Equals(".html"))
                        {
                            return;
                        }
                    }

                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(responseBuilder.ToString());

                    List<string> links = ExtractAllHrefTags(document);

                    Uri uri = new Uri(domain);
                    StringBuilder builder = new StringBuilder();

                    string pattern = $@"(https?:\/\/{uri.Host}\/?)";
                    foreach (var link in links)
                    {
                        try
                        {
                            builder.Clear();
                            if (link.IndexOf("tel:", StringComparison.CurrentCulture) != -1 || link[0].Equals('#'))
                            {
                                continue;
                            }

                            if (link.IndexOf("http", StringComparison.Ordinal) != -1 && Regex.IsMatch(link, pattern))
                            {
                                int idx;
                                string tmpLink = link;
                                if (tmpLink.IndexOf(".css") != -1 ||
                                    tmpLink.IndexOf(".php") != -1 ||
                                    tmpLink.IndexOf(".js") != -1)
                                {
                                    if ((idx = tmpLink.IndexOf("?")) != -1)
                                    {
                                        tmpLink = tmpLink.Substring(0, idx);
                                    }
                                }

                                builder.Append(tmpLink.Substring(Regex.Match(tmpLink, pattern).Groups[0].Value.Length));
                            }
                            else
                            {
                                int idx;
                                string tmpLink = link;
                                idx = tmpLink.IndexOf("#");

                                if (idx != -1)
                                {
                                    tmpLink = tmpLink.Substring(0, idx);
                                }

                                if (tmpLink[0].Equals('/'))
                                {
                                    builder.Append(tmpLink.Substring(1));
                                }
                            }

                            if (!string.IsNullOrEmpty(builder.ToString()))
                            {
                                Parse(builder.ToString(), domain, directory);
                            }
                        }
                        catch 
                        {
                            // ignored
                        }
                    }
                }
                catch (Exception exception)
                {
                    lock (_loker)
                    {
                        File.AppendAllText("Log.txt", $"Date: {DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}\r\n" +
                                                      $"Error: {exception.Message}\r\n" +
                                                      $"CurrentUrl: {currentUrl}\r\n" +
                                                      "--------------------------------------------------------------------------\r\n");
                    }
                }
            });
        }

        private string GetProspectivePath(string url, string fullUrl, DirectoryInfo currentDirectory)
        {
            string path = string.Empty;
            string ext = ".html";

            try
            {
                if (fullUrl.IndexOf("feed") != -1)
                {
                    ext = ".xml";
                }

                string[] strings = url.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string extension = Path.GetExtension(fullUrl);

                if (strings.Length < 2 || (strings.Length == 2 && strings[0].IndexOf("http") != -1))
                {
                    path = !string.IsNullOrEmpty(extension)
                        ? Path.Combine(currentDirectory.FullName, strings[strings.Length - 1])
                        : Path.Combine(currentDirectory.FullName, strings[strings.Length - 1] + ext);
                }
                else
                {
                    for (int i = 0; i < strings.Length; i++)
                    {
                        if (i <= strings.Length - 2)
                        {
                            currentDirectory = currentDirectory.CreateSubdirectory(strings[i]);
                            Directory.CreateDirectory(currentDirectory.FullName);
                        }
                        else if (i == strings.Length - 1)
                        {
                            path = !string.IsNullOrEmpty(extension)
                                ? Path.Combine(currentDirectory.FullName, strings[i])
                                : Path.Combine(currentDirectory.FullName, strings[i] + ext);
                        }
                    }
                }
            }
            catch
            {
                path = string.Empty;
            }

            return path;
        }

        private List<string> ExtractAllHrefTags(HtmlDocument htmlDocument)
        {
            List<string> hrefTagsList = new List<string>();

            foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes("//script[@src]"))
            {
                HtmlAttribute attribute = link.Attributes["src"];
                hrefTagsList.Add(attribute.Value);
            }

            foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes("//link[@href]"))
            {
                HtmlAttribute attribute = link.Attributes["href"];
                hrefTagsList.Add(attribute.Value);
            }

            foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes("//img[@src]"))
            {
                HtmlAttribute attribute = link.Attributes["src"];
                hrefTagsList.Add(attribute.Value);
            }

            foreach (HtmlNode link in htmlDocument.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute attribute = link.Attributes["href"];
                hrefTagsList.Add(attribute.Value);
            }

            return hrefTagsList;
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RootFolderTextBox.Text = dialog.SelectedPath;
            }
        }
    }
}