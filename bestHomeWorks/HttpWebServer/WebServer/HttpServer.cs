using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
    public class HttpServer
    {
        private readonly int _port;
        private readonly TcpListener _tcpListener;
        public bool IsServerWork { get; set; }

        public event WebAppDlg WebAppEvent;

        public HttpServer(int port)
        {
            _port = port;
            _tcpListener = TcpListener.Create(_port);
        }

        public WebAppDlg WebAppDlg
        {
            get => default(WebAppDlg);
            set
            {
            }
        }

        public HttpRequest HttpRequest
        {
            get => default(HttpRequest);
            set
            {
            }
        }

        public HttpResponse HttpResponse
        {
            get => default(HttpResponse);
            set
            {
            }
        }

        public void Run()
        {
            try
            {
                TcpClient tcpClient;
                _tcpListener.Start();

                IsServerWork = true;

                while (IsServerWork)
                {
                    tcpClient = _tcpListener.AcceptTcpClient();

                    string rawRequest = RequestToString(tcpClient);
                    HttpRequest request = new HttpRequest(rawRequest);

                    if (WebAppEvent != null)
                    {
                        HttpResponse httpResponse = WebAppEvent(request);
                        httpResponse.Send(tcpClient);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        public async Task RunAsync()
        {
            try
            {
                TcpClient tcpClient;
                _tcpListener.Start();

                IsServerWork = true;

                while (IsServerWork)
                {
                    tcpClient = await _tcpListener.AcceptTcpClientAsync();

                    string rawRequest = RequestToString(tcpClient);
                    HttpRequest request = new HttpRequest(rawRequest);

                    if (WebAppEvent != null)
                    {
                        HttpResponse httpResponse = WebAppEvent(request);
                        httpResponse.Send(tcpClient);
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        private string RequestToString(TcpClient tcpClient)
        {
            int readBytes;
            byte[] buffer = new byte[512];
            StringBuilder builder = new StringBuilder();
            NetworkStream stream = tcpClient.GetStream();

            do
            {
                readBytes = stream.Read(buffer, 0, buffer.Length);
                builder.Append(Encoding.ASCII.GetString(buffer, 0, readBytes));
            } while (stream.DataAvailable);

            return builder.ToString();
        }

        private async Task<string> RequestToStringAsync(TcpClient tcpClient)
        {
            int readBytes;
            byte[] buffer = new byte[512];
            StringBuilder builder = new StringBuilder();
            NetworkStream stream = tcpClient.GetStream();

            do
            {
                readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                builder.Append(Encoding.ASCII.GetString(buffer, 0, readBytes));
            } while (stream.DataAvailable);

            return builder.ToString();
        }

        public void StopServer()
        {
            try
            {
                if (_tcpListener != null)
                {
                    _tcpListener.Stop();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}