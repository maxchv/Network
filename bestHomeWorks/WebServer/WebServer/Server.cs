using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WebServer
{
    class Server
    {
        private TcpListener _listener;
        public TextBox Logs { get; set; }
        public bool IsWorkServer { get; set; }

        public Server(IPAddress host, int port)
        {
            _listener = new TcpListener(host, port);
        }

        public async void StartServer()
        {
            try
            {
                IsWorkServer = true;

                _listener.Start();

                while (IsWorkServer)
                {
                    Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Waiting for a new client...\r\n";

                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    ProcessingRequest(client);
                }
            }
            catch
            {
                // ignored
            }
        }

        private async void ProcessingRequest(TcpClient tcpClient)
        {
            TcpClient client = tcpClient;

            try
            {
                Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Start conversation with client: {client.Client.RemoteEndPoint}\r\n";

                int readBytes;
                byte[] buffer = new byte[512];
                string request = String.Empty;
                string nameOfRequestedFile = string.Empty;

                NetworkStream stream = client.GetStream();

                do
                {
                    readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    request += Encoding.ASCII.GetString(buffer, 0, readBytes);
                } while (stream.DataAvailable);

                Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Request: {request}\r\n";
                Logs.Text += "-------------------------------------------------------\r\n";

                if (Regex.IsMatch(request, @"GET\s(\/(.+)?)\sHTTP\/\d.\d"))
                {
                    Match match = Regex.Match(request, @"GET\s(\/(.+)?)\sHTTP\/\d.\d");
                    nameOfRequestedFile = match.Groups[2].ToString();
                }

                string filePath = "public_html";
                foreach (var path in nameOfRequestedFile.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    filePath = Path.Combine(filePath, path);
                }

                if (filePath.Equals("public_html"))
                {
                    filePath = Path.Combine("public_html", nameOfRequestedFile);
                }

                if (filePath.Equals("public_html"))
                {
                    filePath = Path.Combine("public_html", "index.html");
                }

                string answer;
                if (File.Exists(filePath))
                {
                    string html = File.ReadAllText(filePath);
                    byte[] bhtml = Encoding.UTF8.GetBytes(html);

                    answer = "HTTP/1.1 200 OK\r\n" +
                             "Server: My http server\r\n" +
                             "Content-Type: text/html; charset=utf-8\r\n" +
                             $"Content-Length: {bhtml.Length}\r\n" +
                             "Connection: Close\r\n" +
                             "\r\n" + html;
                }
                else
                {
                    char sym = '\"';
                    string html = $@"<!doctype html>
<html>
    <head>
        <title>Error 404</title>
        <meta http-equiv={sym}content-type{sym} content={sym}text/html; charset=utf-8{sym} />
    </head>
    <body>
        <div style={sym}text-align: center;margin-top: 10%{sym}>
            <img style={sym}vertical-align: middle;{sym} src={
                            sym
                        }http://sas-network.org/Assets/Images/404.png{sym} alt={sym}Eror 404{sym} width={sym}700{
                            sym
                        } height={sym}420{sym}/>
        </div>
    </body>
</html>";

                    byte[] bhtml = Encoding.UTF8.GetBytes(html);
                    answer = "HTTP/1.1 404 Not Found\r\n" +
                             "Server: My http server\r\n" +
                             "Content-Type: text/html; charset=utf-8\r\n" +
                             $"Content-Length: {bhtml.Length}\r\n" +
                             "Connection: Close\r\n" +
                             "\r\n" +
                             html;
                }

                byte[] response = Encoding.UTF8.GetBytes(answer);
                stream.Write(response, 0, response.Length);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Stop client: {client.Client.RemoteEndPoint}\r\n";
                client.Close();
            }
        }

        public void StopServer()
        {
            try
            {
                if (_listener != null)
                {
                    Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Stop Server: {_listener.LocalEndpoint}\r\n";
                    _listener.Stop();
                }
            }
            catch
            {
                // ignored
            }
        }
    }
}