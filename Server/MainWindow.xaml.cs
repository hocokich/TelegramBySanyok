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
//?
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Markup;
using static Server.MainWindow;
//Первый для удобства, второй для чтения запросов
using static Server.ServerUtility;
using static Server.uRequest;
//Для скачивания лога
using System.IO;
//Удаление лога
using static Server.ClearLog;



namespace Server
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //прослушиваемый порт
        int port = 8888;

        //мой внутренний - 192.168.0.7
        //локальный IP - 127.0.0.1
        string address = "192.168.0.7";

        //объект, прослушивающий порт
        static TcpListener listener;
        //Main поток сервера
        Thread MainlistenThread = null;

        List<SClient> list = new List<SClient>();

        public MainWindow()
        {
            InitializeComponent();
        }

        //функция ожидания и приёма запросов на подключение
        void listen()
        {
            while (true)
            {
                try //принятие запроса на подключение
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => Process(client));
                    clientThread.Start();
                }
                catch { }
            }
        }

        public void Process(TcpClient tcpClient)
        {
            SClient client = new SClient();
            client.client = tcpClient;

            ServerUtility sUtility = new ServerUtility();

            //Проверка на "дудос"
            if (client.client.Available == 0)
            {
                if (client.stream != null)
                    client.stream.Close();

                if (client.client != null)
                    client.client.Close();
            }
            else
            {
                try //означает что в случае возникновении ошибки, управление перейдёт к блоку catch
                {
                    //получение потока для обмена сообщениями
                    client.stream = client.client.GetStream(); //получение канала связи с клиентом
                                                               // буфер для получаемых данных
                    //Добавляет клиента в list
                    SClient sc1 = new SClient();
                    try
                    {
                        sc1.uID = list[list.Count - 1].uID;
                    }
                    catch
                    {
                        sc1.uID = 0;
                    }
                    client.uID = sc1.uID + 1;
                    client.uName = sUtility.GetNameConnection(client);
                    list.Add(client);

                    Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add("Новый клиент подключен: " + client.uName)));
                    //Вывод кол-во онлайн юзеров
                    Dispatcher.BeginInvoke(new Action(() => OnlineClients.Content = list.Count));

                    byte[] data = new byte[256];

                    //цикл ожидания и отправки сообщений
                    while (true)
                    {
                        string message = sUtility.GetMessage(client, data);

                        //вывод сообщения в лог сервера
                        Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(message)));

                        string[] words = message.Split(new char[] { ':' });
                        string Msg = words[1];

                        //Вызываем класс чтения запросов от клиента на сервер
                        uRequest request = new uRequest();

                        //PM-Personal Messages
                        if (Msg.Contains("~PM"))
                        {
                            request.PersonalMessages(Msg, list, data, client);
                            continue;
                        }
                        if (Msg.Contains("~DELFRNDs"))
                        {
                            request.DeleteFriends(Msg, list, data, client);
                            continue;
                        }
                        if (Msg.Contains("~FIND"))
                        {
                            request.FindFriend(Msg, list, data, client);
                            continue;
                        }
                        if (Msg.Contains("~DISCON"))
                        {
                            break;
                        }
                        else//Отправка сообщения в общий канал
                        {
                            if (Msg.Contains("/INV"))
                            {
                                Msg = request.InversionMessage(Msg);
                            }
                            data = Encoding.Unicode.GetBytes(client.uName + ":" + Msg);
                            foreach (SClient sc in list)
                                sc.stream.Write(data, 0, data.Length);
                        }
                    }
                }
                catch (Exception ex) //если возникла ошибка, вывести сообщение об ошибке
                {
                    Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));
                }
                finally //после выхода из бесконечного цикла
                {
                    //Отправка команды клиенту об отключении
                    byte[] data = new byte[14];
                    data = Encoding.Unicode.GetBytes("~DISCON");

                    try
                    {
                        client.stream.Write(data, 0, data.Length);
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));
                    }

                    //освобождение ресурсов при завершении сеанса
                    if (client.stream != null)
                        client.stream.Close();

                    if (client.client != null)
                        client.client.Close();

                    Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(client.uName + ": отключен.")));

                    list.Remove(client);

                    //Вывод кол-ва онлайн юзеров
                    Dispatcher.BeginInvoke(new Action(() => OnlineClients.Content = list.Count));
                }
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            //создание объекта для отслеживания сообщений переданных с ip адреса через порт
            listener = new TcpListener(IPAddress.Parse(address), port);

            //начало прослушивания
            listener.Start();
            ServerLog.Items.Add("Сервер запущен.");
            //создание нового потока для ожидания и подключения клиентов
            MainlistenThread = new Thread(() => listen());
            MainlistenThread.Start();
            
            Dispatcher.BeginInvoke(new Action(() => Start.IsEnabled = false));
            Dispatcher.BeginInvoke(new Action(() => Stop.IsEnabled = true));
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => Stop.IsEnabled = false));
            Dispatcher.BeginInvoke(new Action(() => Start.IsEnabled = true));
            try
            {
                foreach (SClient sc in list)
                {
                    byte[] data = new byte[64];
                    data = Encoding.Unicode.GetBytes("~DISCON");
                    sc.stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));

            }
            try
            {
                listener.Stop();
                MainlistenThread.Abort();
                ServerLog.Items.Add("Сервер остановлен.");
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));
            }

        }

        private void BeforeClose(object sender, EventArgs e)
        {
            try
            {
                foreach (SClient sc in list)
                {
                    byte[] data = new byte[64];
                    data = Encoding.Unicode.GetBytes("~DISCON");
                    sc.stream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));

            }
            try
            {
                listener.Stop();
                MainlistenThread.Abort();
                ServerLog.Items.Add("Сервер остановлен.");
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(new Action(() => ServerLog.Items.Add(ex.Message)));
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            if(ClearLogSign.IsChecked == false)
            {
                ClearLog wnd = new ClearLog();
                if (wnd.ShowDialog() == true)
                {
                    ServerLog.Items.Clear();
                }
            }
            if (ClearLogSign.IsChecked == true)
            {
                ServerLog.Items.Clear();
            }
        }

        private void DownloadLog_Click(object sender, RoutedEventArgs e)
        {
            ServerUtility serverUtility = new ServerUtility();
            serverUtility.DownloadLog(ServerLog);
        }
    }
}