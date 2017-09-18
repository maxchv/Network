using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly Socket _socket;
        private readonly IPEndPoint _ipEndPoint;

        public MainWindow()
        {
            InitializeComponent();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _ipEndPoint = new IPEndPoint(IPAddress.Loopback, 10_002);

            SendScreenShot();
        }

        private void SendScreenShot()
        {
            Task.Run(() =>
            {
                byte[] end = new byte[1000];
                
                while (true)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Bitmap screenShot = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
                        Graphics graphics = Graphics.FromImage(screenShot);

                        graphics.CopyFromScreen(new System.Drawing.Point(0, 0), new System.Drawing.Point(0, 0),
                            screenShot.Size);
                        screenShot.Save("screenshot.bmp");
                    });

                    List<byte[]> packegesList = new List<byte[]>();
                    using (FileStream fsSource = new FileStream("screenshot.bmp", FileMode.Open, FileAccess.Read))
                    {
                        byte[] b = new byte[fsSource.Length];
                        fsSource.Read(b, 0, b.Length);

                        BitmapImage image = LoadImage(b);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ScreenshotImage.Source = image;
                        });

                        fsSource.Position = 0;

                        int readButes;
                        int idOffset = 4;
                        byte[] buffer1 = new byte[996];
                        int indexPackage = 1;

                        while ((readButes = fsSource.Read(buffer1, 0, buffer1.Length)) != 0)
                        {
                            using (MemoryStream stream = new MemoryStream())
                            {
                                stream.Write(BitConverter.GetBytes(indexPackage), 0, idOffset);
                                stream.Write(buffer1, 0, readButes);

                                stream.Position = 0;

                                byte[] buffer2 = new byte[readButes + idOffset];
                                stream.Read(buffer2, 0, readButes + idOffset);

                                packegesList.Add(buffer2);
                                ++indexPackage;
                            }
                        }
                    }

                    for (int i = 0; i < packegesList.Count; i++)
                    {
                        _socket.SendTo(packegesList[i], _ipEndPoint);
                    }

                    _socket.SendTo(end, _ipEndPoint);

                    int time = 5000;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        time = Convert.ToInt32(FrequencyOfSending.Value);
                    });

                    Thread.Sleep(time);
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