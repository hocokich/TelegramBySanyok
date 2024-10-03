using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static Server.ServerUtility;
using System.Windows.Threading;
using static Server.MainWindow;
using System.Windows.Markup;
using System.Windows.Documents;
//
using System.IO;
using System.Windows.Controls;

namespace Server
{
    public class ServerUtility
    {
        public struct SClient
        {
            public int uID;
            public string uName;
            public TcpClient client;
            public NetworkStream stream;
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

        public string GetMessage(SClient client, byte[] data) 
        {
            //объект, для формирования строк
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
            //вывод сообщения в лог сервера
            return message;
        }

        public void DownloadLog(ListBox ServerLog)
        {
            //Получение пути файла
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                //настройка параметров диалога
                FileName = "Document", // Default file name
                DefaultExt = ".txt", // Default file extension
                Filter = "Text documents (.txt)|*.txt" // Filter files by extension


            };
            dlg.ShowDialog();

            //Запись данных в файл
            using (StreamWriter outputFile = new StreamWriter(dlg.FileName))
            {
                int count = ServerLog.Items.Count;

                for (int i = 0; i < count; i++)
                {
                    var slItem = ServerLog.Items[i];
                    outputFile.WriteLine(slItem.ToString());
                }
            }
        }
    }
}