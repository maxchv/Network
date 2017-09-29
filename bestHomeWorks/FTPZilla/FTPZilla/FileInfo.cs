using System;
using System.Windows.Media.Imaging;

namespace FTPZilla
{
    public enum FileType { DIR, FILE }

    public class FileInfo
    {
        public BitmapImage FileImage { get; set; }

        public string FileName { get; set; }

        public string FileSize { get; set; }

        public string FileDateCreation { get; set; }

        public FileType FileType { get; set; }

        public string ParentUri { get; set; }
    }
}