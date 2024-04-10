using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Client.ClientUtility;
using System.Windows.Threading;
using static Client.MainWindow;
using System.Windows.Markup;
using System.Windows.Documents;


namespace Client
{
    public class ClientUtility
    {
        public struct SClient
        {
            public int uID;
            public string uName;
            public TcpClient client;
            public NetworkStream stream;
            public List<string> msgs;
        }
        public string GetNameConnection(SClient client)
        {
            byte[] data = new byte[256];

            StringBuilder builder = new StringBuilder();
            do//до тех пор, пока в потоке есть данные
            {
                //из потока считываются байты и записываются в data начиная с 0
                int bytes = client.stream.Read(data, 0, data.Length);
                //из считанных данных формируется строка
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (client.stream.DataAvailable);
            //преобразование сообщения
            string message = builder.ToString();

            string[] words1 = message.Split(new char[] { ':' });
            client.uName = words1[0];

            data = Encoding.Unicode.GetBytes("~uID:" + client.uID.ToString());
            client.stream.Write(data, 0, data.Length);

            return client.uName;
        }
        public string GetMessage(NetworkStream stream, byte[] data)
        {
            //объект, для формирования строк
            StringBuilder builder = new StringBuilder();

            do//до тех пор, пока в потоке есть данные
            {
                //из потока считываются байты и записываются в data начиная с 0
                int bytes = stream.Read(data, 0, data.Length);
                //из считанных данных формируется строка
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable);
            //преобразование сообщения
            string message = builder.ToString();
            //вывод сообщения в лог сервера
            return message;
        }
        public SClient AddBroadCast()
        {
            SClient friend = new SClient();
            friend.uName = "BroadCast";
            friend.uID = 0;
            friend.msgs = new List<string>();
            return friend;
        }
    }
}
