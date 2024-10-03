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
using System.Xml.Linq;
using System.Windows.Interop;
using System.IO;
//
using static Client.ClientUtility;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Messaging;
using System.Reflection;
//Удаление друзей проверка
using static Client.ClearFriends;
//
using System.Drawing;
using Brushes = System.Windows.Media.Brushes;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //номер порта для обмена сообщениями
        int port = 8888;

        //=======ip адрес сервера======
        //мой внешний ip - 5.137.131.188
        //local IP 127.0.0.1
        string address = "5.137.189.104";

        //объявление TCP клиента
        TcpClient client = null;
        //объявление канала соединения с сервером
        NetworkStream stream = null;
        //Main поток клиента
        Thread MainListenThread = null;
        
        //имя пользователя
        string userName = "";

        List<SClient> friends = new List<SClient>();

        int WhichPMisOpen;//N текущего чата

        public MainWindow()
        {
            InitializeComponent();
        }

        //функция ожидания сообщений от сервера
        void listen()
        {
            try //в случае возникновения ошибки - переход к catch
            {
                //цикл ожидания сообщениями
                while (true)
                {
                    //буфер для получаемых данных
                    byte[] data = new byte[128];

                    //получение сообщения
                    ClientUtility clientUtility = new ClientUtility();
                    string message = clientUtility.GetMessage(stream, data);

                    //Проверка на ключевые слова
                    if (message.Contains("~uID:"))
                    {
                        string[] words = message.Split(new char[] { ':' });
                        Dispatcher.BeginInvoke(new Action(() => myID.Content = words[1]));
                        Dispatcher.BeginInvoke(new Action(() => OnOff.Content = "Есть подключение"));
                        Dispatcher.BeginInvoke(new Action(() => autoriz.Visibility = Visibility.Hidden));
                        Dispatcher.BeginInvoke(new Action(() => ConnectOrDisconnect.Visibility = Visibility.Hidden));
                        Dispatcher.BeginInvoke(new Action(() => uName.Content = userName));
                        continue;
                    }
                    if (message.Contains("~DISCON"))
                    {
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Потеряно соединение с сервером: " + message)));

                        Dispatcher.BeginInvoke(new Action(() => OnOff.Content = "Нет подключения"));
                        Dispatcher.BeginInvoke(new Action(() => myID.Content = "------"));
                        Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.Clear()));

                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.Add("BroadCast")));
                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.SelectedItem = ListOfCorrespondence.Items[0]));


                        break;
                    }
                    if (message.Contains("~NOTFIND"))
                    {
                        MessageBox.Show("Пользователь не найден");
                        continue;
                    }
                    if (message.Contains("~FIND"))
                    {
                        string[] words = message.Split(new char[] { ':' });

                        SClient friend = new SClient
                        {
                            uName = words[0],
                            uID = int.Parse(words[2]),
                            msgs = new List<string>()
                        };
                        Dispatcher.BeginInvoke(new Action(() => friends.Add(friend)));

                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.Add(friend.uName + ":" + friend.uID)));
                        Dispatcher.BeginInvoke(new Action(() => uIDfriend.Text = ""));
                        Dispatcher.BeginInvoke(new Action(() => ClearFriends.IsEnabled = true));

                        continue;
                    }
                    if (message.Contains("~PMFORU"))
                    {
                        string[] words = message.Split(new char[] { ':' });

                        string nameFriend = words[0];
                        int uIDfriend = int.Parse(words[1]);
                        string msgFriend = words[3];

                        //Запись сообщения в коллекцию
                        foreach(SClient sc in friends)
                        {
                            if(sc.uID == uIDfriend)
                            {
                                int Index = friends.IndexOf(sc);
                                friends[Index].msgs.Add(nameFriend + ":" + msgFriend);
                                break;
                            }
                        }

                        if (WhichPMisOpen == uIDfriend)
                        {
                            Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                            Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(uIDfriend)));
                        }

                        continue;
                    }
                    if (message.Contains("~UPM"))
                    {
                        string[] words = message.Split(new char[] { ':' });
                        string myName = words[0];
                        int uIDfriend = int.Parse(words[1]);
                        string myMsg = words[3];

                        foreach (SClient sc in friends)
                        {
                            if (sc.uID == uIDfriend)
                            {
                                int Index = friends.IndexOf(sc);
                                friends[Index].msgs.Add(myName + ":" + myMsg);
                            }
                        }
                        continue;
                    }
                    if (message.Contains("~UDELETED"))
                    {
                        string[] words = message.Split(new char[] { '~' });
                        int uIDfriend = int.Parse(words[0]);

                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.SelectedItem = ListOfCorrespondence.Items[0]));
                        Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                        WhichPMisOpen = 0;
                        Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(WhichPMisOpen)));
                        Dispatcher.BeginInvoke(new Action(() => PMname.Content = "BroadCast"));

                        foreach (SClient sc in friends)
                        {
                            if(sc.uID == uIDfriend)
                            {
                                friends.Remove(sc);

                                string itemList = "";
                                string[] anyWords = null;
                                int uIDfriendInList = 0;

                                int count = ListOfCorrespondence.Items.Count;

                                for (int i = 1; i <= count; i++)
                                {
                                    itemList = ListOfCorrespondence.Items[i].ToString();
                                    anyWords = itemList.Split(new char[] { ':' });
                                    uIDfriendInList = int.Parse(anyWords[1]);

                                    if(uIDfriendInList == uIDfriend)
                                    {
                                        Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.RemoveAt(i)));

                                        break;
                                    }
                                }
                                break;
                            }
                        }
                        if (ListOfCorrespondence.Items.Count > 1)
                        {
                            Dispatcher.BeginInvoke(new Action(() => ClearFriends.IsEnabled = false));
                        }
                        
                        continue;
                    }
                    else
                    {
                        //вывод сообщения в лог клиента
                        Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                        Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(WhichPMisOpen)));
                        //Сохранение сообщений BroadCast
                        friends[0].msgs.Add(message);
                    }
                }
            }
            catch (Exception ex)
            {
                //вывести сообщение об ошибке
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
            }
            finally
            {
                //закрыть канал связи и завершить работу клиента
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
                if (client != null)
                {
                    client.Close();
                    client = null;
                }

                friends.Clear();

                MainListenThread.Abort();
            }
        }
        void LastMsgWithFriend(int uIDfriend)
        {
            foreach (SClient sc in friends)
            {
                if (sc.uID == uIDfriend)
                {
                    try
                    {
                        int Index = friends.IndexOf(sc);
                        foreach (string oneMsg in friends[Index].msgs)
                            LogMessages.Items.Add(oneMsg);
                    }
                    catch { }
                    break;
                }
            }
        }
        void ClearFriendsSimple()
        {
            ListOfCorrespondence.Items.Clear();
            Dispatcher.BeginInvoke(new Action(() => ClearFriends.IsEnabled = false));

            foreach (SClient sc in friends)
            {
                if (sc.uID != 0)
                {
                    string message = userName + ":~DELFRNDs~" + sc.uID;
                    //преобразование сообщение в массив байтов
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    //отправка сообщения

                    if (stream != null)
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Dispatcher.BeginInvoke(new Action(() => friends.Remove(sc)));
                    break;
                }
            }

            Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.Add("BroadCast")));
            Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.SelectedItem = ListOfCorrespondence.Items[0]));
            Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(0)));
        }

        private void EnterSend(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Send_Click(sender, e);
            }
        }
        private void Send_Click(object sender, RoutedEventArgs e)
        {
            int d = ListOfCorrespondence.SelectedIndex;
            if (d == 0)
            {
                try
                {
                    //получение сообщения
                    string message = msg.Text;
                    //добавление имени пользователя к сообщению
                    message = String.Format("{0}: {1} ", userName, message);
                    //преобразование сообщение в массив байтов
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    if (data.Count() > 256)
                    {
                        //вывести сообщение об ошибке
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Слишком длинное сообщение")));
                        return;
                    }
                    //отправка сообщения
                    stream.Write(data, 0, data.Length);
                    msg.Text = "";
                }
                catch (Exception ex)//вывести сообщение об ошибке
                {
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
                }
            }
            else
            {
                try
                {
                    string[] words = ListOfCorrespondence.SelectedItem.ToString().Split(new char[] { ':' });
                    string friendName = words[0];
                    string uIDfriend = words[1];

                    string message = msg.Text + "~PM~" + uIDfriend;
                    message = String.Format("{0}: {1} ", userName, message);
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    //Проверка
                    if (data.Count() > 256)
                    {
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Слишком длинное сообщение")));
                        return;
                    }
                    //отправка сообщения
                    stream.Write(data, 0, data.Length);
                    msg.Text = "";

                    LogMessages.Items.Clear();
                    LastMsgWithFriend(int.Parse(uIDfriend));
                }
                catch (Exception ex)
                {
                    //вывести сообщение об ошибке
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
                }
            }

        }

        int Click = 0;
        private void ConnectOrDisconnect_Click(object sender, RoutedEventArgs e)
        {
            //Connect
            if (Click == 0)
            {
                if (client == null)
                {
                    ClientUtility cUtility = new ClientUtility();

                    Dispatcher.BeginInvoke(new Action(() => friends.Add(cUtility.AddBroadCast())));

                    //получение имени пользователя
                    userName = autoriz.name.Text;
                    try //если возникнет ошибка - переход в catch
                    {
                        //проверка на длину ника
                        if (userName.Length > 20 || userName.Length < 3)
                        {
                            //вывести сообщение об ошибке
                            MessageBox.Show("Неподходящий никнейм");
                            return;
                        }
                        //создание клиента
                        client = new TcpClient(address, port);
                        //получение канала для обмена сообщениями
                        stream = client.GetStream();
                        //отправка сообщения
                        string message = userName + ": ~CON";
                        //преобразование сообщение в массив байтов
                        byte[] data = Encoding.Unicode.GetBytes(message);
                        //отправка сообщения
                        stream.Write(data, 0, data.Length);

                        //создание нового потока для ожидания и подключения клиента
                        MainListenThread = new Thread(() => listen());
                        MainListenThread.Start();
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
                    }
                }
                ConnectOrDisconnect.Content = "Отключиться";
                Click = 1;
                return;
            }
            //Disconnect
            if (Click == 1)
            {
                try
                {
                    ClearFriendsSimple();

                    //получение сообщени
                    string message = userName + ": ~DISCON";
                    //преобразование сообщение в массив байтов
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    //отправка сообщения

                    if (stream != null)
                    {
                        stream.Write(data, 0, data.Length);
                    }
                    Dispatcher.BeginInvoke(new Action(() => OnOff.Content = "Нет подключения"));
                    Dispatcher.BeginInvoke(new Action(() => myID.Content = "------"));

                    Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                    Dispatcher.BeginInvoke(new Action(() => ListOfCorrespondence.Items.Clear()));

                    friends.Clear();
                }
                catch (Exception ex)
                {
                    //вывести сообщение об ошибке
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
                }
                ConnectOrDisconnect.Content = "Подключиться";
                Click = 0;
                return;
            }
        }

        private void BeforeClose(object sender, EventArgs e)
        {
            try
            {
                ClearFriendsSimple();

                //получение сообщения
                string message = userName + ": ~DISCON";
                //преобразование сообщение в массив байтов
                byte[] data = Encoding.Unicode.GetBytes(message);
                //отправка сообщения
                stream?.Write(data, 0, data.Length);//? - проверка соединения
            }
            catch (Exception ex)
            {
                //вывести сообщение об ошибке
                Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Ошибка: " + ex.Message)));
            }
        }

        private void FindFriend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                for (int i = 0; i < friends.Count - 1; i++)
                {
                    if (int.Parse(uIDfriend.Text) == friends[i].uID) return;
                }

                int uIDfriendFind = int.Parse(uIDfriend.Text);
                string myUid = myID.Content.ToString();
                if (uIDfriendFind >= 1000 || uIDfriendFind == int.Parse(myUid))
                {
                    Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Некорректный идентификатор")));
                }

                else
                {
                    string message = userName + ":~FIND~" + uIDfriend.Text;
                    byte[] data = Encoding.Unicode.GetBytes(message);
                    stream?.Write(data, 0, data.Length);//? - проверка соединения
                }
            }
            catch { Dispatcher.BeginInvoke(new Action(() => MessageBox.Show("Некорректный идентификатор"))); }
        }

        private void ListOfCorrespondence_Click(object sender, SelectionChangedEventArgs e)
        {
            int SelIndx = ListOfCorrespondence.SelectedIndex;
            if (SelIndx == 0)
            {
                WhichPMisOpen = 0;
                Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(0)));
                Dispatcher.BeginInvoke(new Action(() => PMname.Content = "BroadCast"));
            }
            else
            {
                Dispatcher.BeginInvoke(new Action(() => LogMessages.Items.Clear()));
                if(ListOfCorrespondence.SelectedItem != null)
                {
                    string SelPM = ListOfCorrespondence.SelectedItem.ToString();
                    string[] words = SelPM.Split(new char[] { ':' });
                    string friendName = words[0];
                    int uIDfriend = int.Parse(words[1]);
                    WhichPMisOpen = uIDfriend;
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() => LastMsgWithFriend(uIDfriend)));
                        Dispatcher.BeginInvoke(new Action(() => PMname.Content = SelPM));
                    }
                    catch { }
                }
            }
        }

        private void ClearFriends_Click(object sender, RoutedEventArgs e)
        {
            ClearFriends wnd = new ClearFriends();
            if(ClearFriendSign.IsChecked == false)
            {
                if (wnd.ShowDialog() == true)
                {
                    ClearFriendsSimple();
                }
            }
            if (ClearFriendSign.IsChecked == true)
            {
                ClearFriendsSimple();
            }
        }

    }
}