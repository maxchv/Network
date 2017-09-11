
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    //переменные, необходимые для настройки подключения:
    //удаленный хост и порты - удаленный и локальный
    static int RemotePort;
    static int LocalPort;
    static IPAddress RemoteIPAddr;

    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Console.SetWindowSize(40, 20);
            Console.Title = "Chat";
            Console.WriteLine("введите удаленный IP");
            RemoteIPAddr = IPAddress.Parse(Console.ReadLine());
            Console.WriteLine("введите удаленный порт");
            RemotePort = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("введите локальный порт");
            LocalPort = Convert.ToInt32(Console.ReadLine());
            //отдельный поток для чтения в методе ThreadFuncReceive
            //этот метод вызывает метод Receive() класса UdpClient,
            //который блокирует текущий поток, поэтому необходим отдельный
            //поток
            Thread thread = new Thread(
                   new ThreadStart(ThreadFuncReceive)
            );
            //создание фонового потока
            thread.IsBackground = true;
            //запуск потока
            thread.Start();
            Console.ForegroundColor = ConsoleColor.Red;
            while (true)
            {
                SendData(Console.ReadLine());
            }
        }
        catch (FormatException formExc)
        {
            Console.WriteLine("Преобразование невозможно :" + formExc);
        }
        catch (Exception exc)
        {
            Console.WriteLine("Ошибка : " + exc.Message);
        }
    }

    static void ThreadFuncReceive()
    {
        try
        {
            while (true)
            {
                //подключение к локальному хосту
                UdpClient uClient = new UdpClient(LocalPort);
                IPEndPoint ipEnd = null;
                //получание дейтаграммы
                byte[] responce = uClient.Receive(ref ipEnd);
                //преобразование в строку
                string strResult = Encoding.Unicode.GetString(responce);
                Console.ForegroundColor = ConsoleColor.Green;
                //вывод на экран
                Console.WriteLine(strResult);
                Console.ForegroundColor = ConsoleColor.Red;
                uClient.Close();
            }
        }
        catch (SocketException sockEx)
        {
            Console.WriteLine("Ошибка сокета: " + sockEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка : " + ex.Message);
        }
    }

    static void SendData(string datagramm)
    {
        UdpClient uClient = new UdpClient();
        //подключение к удаленному хосту
        IPEndPoint ipEnd = new IPEndPoint(RemoteIPAddr, RemotePort);
        try
        {
            byte[] bytes = Encoding.Unicode.GetBytes(datagramm);
            uClient.Send(bytes, bytes.Length, ipEnd);
        }
        catch (SocketException sockEx)
        {
            Console.WriteLine("Ошибка сокета: " + sockEx.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка : " + ex.Message);
        }
        finally
        {
            //закрытие экземпляра класса UdpClient
            uClient.Close();
        }
    }
}


