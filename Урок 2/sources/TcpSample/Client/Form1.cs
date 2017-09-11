
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        TcpClient client;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //создание экземпляра класса IPEndPoint
                IPEndPoint endPoint = new IPEndPoint(
                    IPAddress.Parse(textBox1.Text),
                    Convert.ToInt32(textBox3.Text)
                );
                client = new TcpClient();
                //установка соединения с использованием 
                //данных IP и номера порта
                client.Connect(endPoint);

                //получение сетевого потока
                NetworkStream nstream = client.GetStream();
                //преобразование строки сообщения в массив байт
                byte[] barray = Encoding.Unicode.GetBytes(textBox2.Text);
                //запись в сетевой поток всего массива 
                nstream.Write(barray, 0, barray.Length);
                //закрытие клиента
                client.Close();
            }
            catch (SocketException sockEx)
            {
                MessageBox.Show("Ошибка сокета:" + sockEx.Message);
            }
            catch (Exception Ex)
            {
                MessageBox.Show("Ошибка :" + Ex.Message);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (client != null)
                client.Close();
        }
    }
}
