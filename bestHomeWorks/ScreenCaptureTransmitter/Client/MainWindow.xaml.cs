using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private Socket _socket;
        private IPEndPoint _ipEndPoint;
        private EndPoint _remoteEP;

        public MainWindow()
        {
            InitializeComponent();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _ipEndPoint = new IPEndPoint(IPAddress.Loopback, 10_002);
            _socket.Bind(_ipEndPoint);
            _remoteEP = new IPEndPoint(IPAddress.Any, 0);

            RunListener();
        }

        private void RunListener()
        {
            Task.Run(() =>
            {
                List<byte[]> packages = new List<byte[]>();

                byte[] buff = new byte[1000];
                int len = 0;

                while (true)
                {
                    len = _socket.ReceiveFrom(buff, ref _remoteEP);

                    using (MemoryStream stream2 = new MemoryStream())
                    {
                        stream2.Write(buff, 0, len);

                        stream2.Position = 0;

                        byte[] buffer = new byte[len];
                        stream2.Read(buffer, 0, len);

                        int idx1 = BitConverter.ToInt32(new[] { buffer[0], buffer[1], buffer[2], buffer[3] }, 0);

                        if (idx1 == 0)
                        {
                            using (MemoryStream stream = new MemoryStream())
                            {
                                foreach (byte[] arr in packages)
                                {
                                    stream.Write(arr, 4, arr.Length - 4);
                                }

                                byte[] buffer2 = stream.ToArray();
                                stream.Read(buffer2, 0, buffer2.Length);

                                BitmapImage image = LoadImage(buffer2);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ScreenshotImage.Source = image;
                                });
                            }

                            packages.Clear();
                        }
                        else
                        {
                            packages.Add(buffer);
                        }
                    }

                    //packages.Sort(delegate (byte[] arr1, byte[] arr2)
                    //{
                    //    int idx1 = BitConverter.ToInt32(new[] { arr1[0], arr1[1], arr1[2], arr1[3] }, 0);
                    //    int idx2 = BitConverter.ToInt32(new[] { arr2[0], arr2[1], arr2[2], arr2[3] }, 0);

                    //    return idx1 > idx2 ? 1 : -1;
                    //});
                }
            });
        }

        private BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}
