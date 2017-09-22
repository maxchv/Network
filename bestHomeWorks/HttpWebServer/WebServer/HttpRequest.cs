using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;

namespace WebServer
{
    public class HttpRequest
    {
        public string QueryString { get; set; }

        public string StartString { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        public string Body { get; set; }

        public string Url { get; set; }

        public string ContentType { get; set; }

        public HttpMethod HttpMethod { get; set; } = HttpMethod.GET;

        public HttpRequest(string rawRequest)
        {
            string[] allRequest = rawRequest.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            StartString = allRequest[0];
            Match m = Regex.Match(StartString, @"^(?<method>.+?)\s+(?<url>.+?)\s+(?<version>.+)$");

            QueryString = rawRequest;
            Url = m.Groups["url"].Value;

            switch (m.Groups["method"].Value.ToUpper())
            {
                case "GET":
                    HttpMethod = HttpMethod.GET;
                    break;
                case "POST":
                    HttpMethod = HttpMethod.POST;
                    break;
                case "PUT":
                    HttpMethod = HttpMethod.PUT;
                    break;
                case "OPTIONS":
                    HttpMethod = HttpMethod.OPTIONS;
                    break;
                case "DELETE":
                    HttpMethod = HttpMethod.DELETE;
                    break;
                case "HEAD":
                    HttpMethod = HttpMethod.HEAD;
                    break;
                case "PATCH":
                    HttpMethod = HttpMethod.PATCH;
                    break;
                case "TRACE":
                    HttpMethod = HttpMethod.TRACE;
                    break;
                case "CONNECT":
                    HttpMethod = HttpMethod.CONNECT;
                    break;
            }

            int indexEmptyString = allRequest.Length;
            for (int i = 0; i < allRequest.Length; i++)
            {
                if (string.IsNullOrEmpty(allRequest[i]))
                {
                    indexEmptyString = i;
                }

                if (i != 0 && i < indexEmptyString)
                {
                    int index = allRequest[i].IndexOf(":", StringComparison.Ordinal);

                    if (index != -1)
                    {
                        Headers.Add(allRequest[i].Substring(0, index), allRequest[i].Substring(index + 2));
                    }
                }

                if (i == indexEmptyString + 1)
                {
                    var urlDecode = HttpUtility.UrlDecode(allRequest[i]);
                    if (urlDecode != null)
                    {
                        ParseParameters(urlDecode);
                    }
                }

                if (i > indexEmptyString)
                {
                    Body += allRequest[i];
                }
            }

            if (HttpMethod == HttpMethod.GET)
            {
                int index = Url.IndexOf("?", StringComparison.Ordinal);
                if (index != -1)
                {
                    ParseParameters(HttpUtility.UrlDecode(Url.Substring(index + 1)));
                }
            }
        }

        private void ParseParameters(string urlDecode)
        {
            string[] parameters = urlDecode.Split('&');
            foreach (var parameter in parameters)
            {
                int index = parameter.IndexOf("=", StringComparison.Ordinal);

                if (index != -1)
                {
                    Parameters.Add(parameter.Substring(0, index),
                        parameter.Substring(index + 1));
                }
            }
        }
    }
}