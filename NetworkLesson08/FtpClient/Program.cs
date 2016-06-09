using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FtpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            DownloadData();
        }

        static void DownloadData()
        {
            FtpWebRequest req = WebRequest.Create("ftp://127.0.0.1/c_sharp2.pdf") as FtpWebRequest;
            req.Credentials = new NetworkCredential("max", "max");
            req.Method = WebRequestMethods.Ftp.DownloadFile;           

            FtpWebResponse resp = req.GetResponse() as FtpWebResponse;
            Console.WriteLine("StatusCode: {0}", resp.StatusCode);
            using (Stream reader = resp.GetResponseStream())
            {
                int count = 0;
                byte[] buff = new byte[1024];
                using (Stream stream = new FileStream("c_sharp2.pdf", FileMode.Create, FileAccess.Write, FileShare.None))
                {                    
                    do
                    {
                        count = reader.Read(buff, 0, buff.Length);
                        stream.Write(buff, 0, count);
                    } while (count > 0);
                }
            }
        }

        static void UploadData()
        {
            FtpWebRequest req = WebRequest.Create("ftp://127.0.0.1/folder.png") as FtpWebRequest;
            req.Credentials = new NetworkCredential("max", "max");
            req.Method = WebRequestMethods.Ftp.UploadFile;
            using (Stream writer = req.GetRequestStream())
            {
                byte[] png = File.ReadAllBytes("folder.png");
                writer.Write(png, 0, png.Length);
            }

            FtpWebResponse resp = req.GetResponse() as FtpWebResponse;
            Console.WriteLine("StatusCode: {0}", resp.StatusCode);
            //using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
            //{
            //    Console.WriteLine(reader.ReadToEnd());
            //}
        }
    }
}
