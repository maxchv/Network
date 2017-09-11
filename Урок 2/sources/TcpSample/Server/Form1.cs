using System;
using System.Threading;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        TcpListener list;
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //создание экземпляра класса TcpListener
                //данные о хосте и порте читаются 
                //из текстовых окон
                list = new TcpListener(
                    IPAddress.Parse(textBox1.Text),
                    Convert.ToInt32(textBox2.Text));
                //начало прослушивания клиентов
                list.Start();
                //создание отдельного потока для чтения сообщения
                Thread thread = new Thread(
                        new ThreadStart(ThreadFun)
                        );
                thread.IsBackground = true;
                //запуск потока
                thread.Start();
            }
            catch (SocketException sockEx)
            {
                MessageBox.Show("Ошибка сокета: " + sockEx.Message);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Ошибка : " + Ex.Message);
            }
        }

        void ThreadFun()
        {
            while (true)
            {
                //сервер сообщает клиенту о готовности 
                //к соединению
                TcpClient cl = list.AcceptTcpClient();
                //чтение данных из сети в формате Unicode
                StreamReader sr = new StreamReader(
                    cl.GetStream(), Encoding.Unicode
                );
                string s = sr.ReadLine();
                //добавление полученного сообщения в список 
                messageList.Items.Add(s);
                cl.Close();
                //при получении сообщения EXIT завершить приложение
                if (s.ToUpper() == "EXIT")
                {
                    list.Stop();
                    this.Close();
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (list != null)
                list.Stop();
        }
    }
}
