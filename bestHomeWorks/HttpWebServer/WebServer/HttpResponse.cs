using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace WebServer
{
    public class HttpResponse
    {
        public string Body { get; set; } = default(string);

        public string ContentType { get; set; } = default(string);

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public string StatusCode { get; set; } = default(string);

        public async void Send(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();

                StringBuilder builder = new StringBuilder();

                builder.Append($"HTTP/1.1 {StatusCode}\r\n");

                foreach (var header in Headers)
                {
                    builder.Append($"{header.Key}: {header.Value}\r\n");
                }

                builder.Append($"Content-Type: {ContentType}\r\n");
                builder.Append("\r\n");
                builder.Append(Body);

                byte[] response = Encoding.UTF8.GetBytes(builder.ToString());
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                client.Close();
            }
        }

        public Dictionary<int, string> StatusCodes = new Dictionary<int, string>()
        {
            { 200, "200 OK" },
            { 201, "201 Created" },
            { 204, "204 No Content" },
            { 302, "302 Found" },
            { 400, "400 Bad Request" },
            { 404, "404 Not Found" }
        };
    }
}