using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//подключение библиотек для работы с сетью и потоками
using System.Net;
using System.Net.Sockets;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;


namespace Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //прослушиваемый порт
        int port = 8888;
        //объект, прослушивающий порт
        static TcpListener listener;
        //Main поток сервера
        Thread MainlistenThread = null;

        public MainWindow()
        {
            InitializeComponent();
        }
        //функция ожидания и приёма запросов на подключение
        void listen()
        {
            //цикл подключения клиентов
            while (true)
            {
                //принятие запроса на подключение
                TcpClient client = listener.AcceptTcpClient();

                //создание нового потока для обслуживания нового клиента
                Thread clientThread = new Thread(() => Process(client));
                clientThread.Start();
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            //создание объекта для отслеживания сообщений переданных с ip адреса через порт
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            //начало прослушивания
            listener.Start();

            ServerLog.Items.Add("Сервер запущен.");

            //создание нового потока для ожидания и подключения клиентов
            MainlistenThread = new Thread(() => listen());
            MainlistenThread.Start();
        }
        //обработка сообщений от клиента
        public void Process(TcpClient tcpClient)
        {
            TcpClient client = tcpClient;
            NetworkStream stream = null; //получение канала связи с клиентом

            try //означает что в случае возникновении ошибки, управление перейдёт к блоку catch
            {
                //получение потока для обмена сообщениями
                stream = client.GetStream(); //получение канала связи с клиентом
                                             // буфер для получаемых данных
                
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add("Новый клиент подключен.")));

                byte[] data = new byte[64];
                //цикл ожидания и отправки сообщений
                while (true)
                {
                    //==========================получение сообщения============================
                    //объект, для формирования строк
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    //до тех пор, пока в потоке есть данные
                    do
                    {
                        //из потока считываются 64 байта и записываются в data начиная с 0
                        bytes = stream.Read(data, 0, data.Length);
                        //из считанных данных формируется строка
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    //преобразование сообщения
                    string message = builder.ToString();

                    //Проверка на запрос инверсии сообщения
                    if (message.Contains('~'))
                    {
                        string[] words = message.Split(new char[] { ':' });
                        string uName = words[0] + ": ";
                        string Msg = words[1];
                        Msg = Msg.Replace("~", "");
                        string RevMsg = new string(Msg.Reverse().ToArray());
                        data = Encoding.Unicode.GetBytes(uName + RevMsg);
                        //отправка сообщения обратно клиенту
                        stream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        //преобразование сообщения в набор байт
                        data = Encoding.Unicode.GetBytes(message);
                        //отправка сообщения обратно клиенту
                        stream.Write(data, 0, data.Length);
                    }

                    //вывод сообщения в лог сервера
                    Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(message)));
                    //==========================отправка сообщения=============================
                    //преобразование сообщения в набор байт
                    //data = Encoding.Unicode.GetBytes(message);
                    //отправка сообщения обратно клиенту
                    //stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex) //если возникла ошибка, вывести сообщение об ошибке
            {
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));
            }
            finally //после выхода из бесконечного цикла
            {
                //освобождение ресурсов при завершении сеанса
                if (stream != null)
                    stream.Close();
                if (client != null)
                    client.Close();
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            listener.Stop();
            MainlistenThread.Abort();
            ServerLog.Items.Add("Сервер остановлен.");
        }
    }
}
