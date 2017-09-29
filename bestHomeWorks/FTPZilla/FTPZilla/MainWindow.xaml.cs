using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MahApps.Metro.Controls.Dialogs;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace FTPZilla
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private string _host = String.Empty;
        private string _login = String.Empty;
        private string _password = String.Empty;
        private ObservableCollection<FileInfo> _files;

        public MainWindow()
        {
            InitializeComponent();

            _files = new ObservableCollection<FileInfo>();
            FilesListView.ItemsSource = _files;
        }

        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            _host = HostTextBox.Text;
            _login = LoginTextBox.Text;
            _password = PasswordPasswordBox.Password;

            _files.Clear();
            SetItems(SetItemsCurrentUri(_host));
        }

        private void SetItems(ObservableCollection<FileInfo> itemsList)
        {
            try
            {
                foreach (FileInfo file in itemsList)
                {
                    _files.Add(file);
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private List<string> GetListDirectory(string uri)
        {
            List<string> list = new List<string>();

            try
            {
                FtpWebRequest request = CreateRequest(uri, WebRequestMethods.Ftp.ListDirectoryDetails);

                FtpWebResponse ftpWebResponse = request.GetResponse() as FtpWebResponse;

                if (ftpWebResponse != null)
                {
                    using (Stream stream = ftpWebResponse.GetResponseStream())
                    {
                        if (stream != null)
                        {
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                while (!reader.EndOfStream)
                                {
                                    list.Add(reader.ReadLine());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }

            return list;
        }

        private FtpWebRequest CreateRequest(string uri, string method)
        {
            FtpWebRequest ftpWebRequest = WebRequest.Create(uri) as FtpWebRequest;

            ftpWebRequest.Credentials = new NetworkCredential(_login, _password);
            ftpWebRequest.Method = method;
            ftpWebRequest.UsePassive = true;

            return ftpWebRequest;
        }

        private void FilesListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileInfo file = FilesListView.SelectedItem as FileInfo;

            try
            {
                if (file != null)
                {
                    if (file.FileType == FileType.DIR)
                    {
                        if (file.FileName.Equals("..") &&
                            file.FileImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BackArrowIcon.png")))
                        {
                            int index;
                            index = file.ParentUri.LastIndexOf("/");

                            if (index != -1 && index != 4 && index != 5)
                            {
                                _files.Clear();
                                SetItems(SetItemsCurrentUri(file.ParentUri.Substring(0, index)));
                            }
                        }
                        else
                        {
                            _files.Clear();
                            SetItems(SetItemsCurrentUri(Path.Combine(file.ParentUri, file.FileName)
                                .Replace("\\", "/")));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private async void RemoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            string uri = string.Empty;

            foreach (var item in FilesListView.SelectedItems)
            {
                FileInfo file = item as FileInfo;

                try
                {
                    if (file != null)
                    {
                        if (!file.FileName.Equals("..") &&
                            !file.FileImage.UriSource.Equals(new Uri("pack://application:,,,/Images/BackArrowIcon.png")))
                        {
                            if (file.FileType == FileType.FILE)
                            {
                                FtpWebRequest request = CreateRequest(
                                    Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"),
                                    WebRequestMethods.Ftp.DeleteFile);

                                StatusBar.Text = getStatusDescription(request);

                                uri = file.ParentUri;
                            }
                            else
                            {
                                RemoveDirectory(Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"));

                                FtpWebRequest request = CreateRequest(
                                    Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"),
                                    WebRequestMethods.Ftp.RemoveDirectory);

                                StatusBar.Text = getStatusDescription(request);

                                uri = file.ParentUri;
                            }
                        }
                    }
                    else
                    {
                        await this.ShowMessageAsync("Удаление", "Пожалуйста выберите элемент который хотите удалить!");
                    }
                }
                catch (Exception exception)
                {
                    StatusBar.Text = exception.Message;
                }
            }

            if (!string.IsNullOrEmpty(uri))
            {
                _files.Clear();
                SetItems(SetItemsCurrentUri(uri));
            }
        }

        private void RemoveDirectory(string uri)
        {
            ObservableCollection<FileInfo> files = SetItemsCurrentUri(uri);

            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.FileType == FileType.FILE)
                    {
                        FtpWebRequest request = CreateRequest(
                            Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"),
                            WebRequestMethods.Ftp.DeleteFile);

                        getStatusDescription(request);
                    }
                    else
                    {
                        if (!file.FileName.Equals("..") &&
                            !file.FileImage.UriSource.Equals(
                                new Uri("pack://application:,,,/Images/BackArrowIcon.png")))
                        {
                            RemoveDirectory(Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"));

                            FtpWebRequest request = CreateRequest(
                                Path.Combine(file.ParentUri, file.FileName).Replace("\\", "/"),
                                WebRequestMethods.Ftp.RemoveDirectory);

                            StatusBar.Text = getStatusDescription(request);
                        }
                    }
                }
                catch (Exception exception)
                {
                    StatusBar.Text = exception.Message;
                }
            }
        }

        private string getStatusDescription(FtpWebRequest request)
        {
            try
            {
                using (var response = (FtpWebResponse)request.GetResponse())
                {
                    return response.StatusDescription;
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private void FilesListView_OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void FilesListView_OnDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            try
            {
                if (_files.Count > 0)
                {
                    string uri = _files[0].ParentUri;
                    foreach (string file in files)
                    {
                        string fileName = Path.GetFileName(file);

                        UploadFile(uri, fileName, file);
                    }

                    _files.Clear();
                    SetItems(SetItemsCurrentUri(uri));
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private async void UploadFile(string uri, string fileName, string filePath)
        {
            try
            {
                FtpWebRequest request = CreateRequest(Path.Combine(uri, fileName).Replace("\\", "/"),
                    WebRequestMethods.Ftp.UploadFile);

                using (Stream stream = request.GetRequestStream())
                {
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        int readBytes;

                        byte[] buffer = new byte[1000];

                        while ((readBytes = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await stream.WriteAsync(buffer, 0, readBytes);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private void UploadFilesMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "All Files (*.*)|*.*";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                if (_files.Count > 0)
                {
                    try
                    {
                        string uri = _files[0].ParentUri;
                        foreach (string file in dialog.FileNames)
                        {
                            string fileName = Path.GetFileName(file);

                            UploadFile(uri, fileName, file);
                        }

                        _files.Clear();
                        SetItems(SetItemsCurrentUri(uri));
                    }
                    catch (Exception exception)
                    {
                        StatusBar.Text = exception.Message;
                    }
                }
            }
        }

        private void DownloadMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                foreach (var item in FilesListView.SelectedItems)
                {
                    try
                    {
                        FileInfo file = item as FileInfo;

                        if (file != null)
                        {
                            if (!file.FileName.Equals("..") &&
                                !file.FileImage.UriSource.Equals(
                                    new Uri("pack://application:,,,/Images/BackArrowIcon.png")))
                            {
                                if (file.FileType == FileType.FILE)
                                {
                                    DownloadFile(file.ParentUri, file.FileName, path);
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        StatusBar.Text = exception.Message;
                    }
                }
            }
        }

        private async void DownloadFile(string uri, string fileName, string path)
        {
            try
            {
                FtpWebRequest request = CreateRequest(Path.Combine(uri, fileName).Replace("\\", "/"),
                    WebRequestMethods.Ftp.DownloadFile);

                FtpWebResponse ftpWebResponse = (FtpWebResponse)request.GetResponse();

                using (Stream stream = ftpWebResponse.GetResponseStream())
                {
                    using (FileStream fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Create, FileAccess.Write))
                    {
                        int readBytes;

                        byte[] buffer = new byte[1000];

                        while ((readBytes = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, readBytes);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                StatusBar.Text = exception.Message;
            }
        }

        private ObservableCollection<FileInfo> SetItemsCurrentUri(string uri)
        {
            Regex regex = new Regex(
                @"^(?<Type>[d-])([rwxt-]{3}){3}\s+\d{1,}\s+.*?(?<Size>\d{1,})\s+(?<Date>\w+\s+\d{1,2}\s+(?:\d{4})?)(?<Time>\d{1,2}:\d{2})?\s+(?<Name>.+?)\s?$",
                RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase |
                RegexOptions.IgnorePatternWhitespace);


            ObservableCollection<FileInfo> files = new ObservableCollection<FileInfo>();
            FileInfo emptyFile = new FileInfo();
            emptyFile.FileName = "..";
            emptyFile.FileSize = string.Empty;
            emptyFile.FileType = FileType.DIR;
            emptyFile.ParentUri = uri;
            emptyFile.FileImage = new BitmapImage(new Uri("pack://application:,,,/Images/BackArrowIcon.png"));

            files.Add(emptyFile);

            foreach (string fileString in GetListDirectory(uri))
            {
                try
                {
                    Match match = regex.Match(fileString);

                    if (match.Length > 5)
                    {
                        FileInfo file = new FileInfo();

                        string type = match.Groups["Type"].Value == "d" ? "dir" : "file";
                        file.FileType = type.Equals("file") ? FileType.FILE : FileType.DIR;

                        file.FileSize = file.FileType == FileType.FILE
                            ? match.Groups["Size"].Value + " байт"
                            : String.Empty;
                        file.FileImage = new BitmapImage(file.FileType == FileType.FILE
                            ? new Uri("pack://application:,,,/Images/File.png")
                            : new Uri("pack://application:,,,/Images/Folder.png"));

                        file.FileName = match.Groups["Name"].Value;
                        file.FileDateCreation = match.Groups["Date"].Value + match.Groups["Time"].Value;
                        file.ParentUri = uri;

                        files.Add(file);
                    }
                }
                catch (Exception exception)
                {
                    StatusBar.Text = exception.Message;
                }
            }

            return files;
        }
    }
}