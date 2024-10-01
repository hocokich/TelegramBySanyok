using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Markup;

namespace Server
{
    internal class uRequest: ServerUtility
    {//Utility Request - утилита запросов
        public string InversionMessage(string Msg)
        {
            Msg = Msg.Replace("/INV", "");
            string RevMsg = new string(Msg.Reverse().ToArray());
            return RevMsg;
        }

        public void PersonalMessages(string Msg, List<SClient> list, byte[] data, SClient client)
        {
            string[] words1 = Msg.Split(new char[] { '~' });
            string msgforFriend = words1[0];
            int uIDfriend = int.Parse(words1[2]);

            if (msgforFriend.Contains("/INV"))
            {
                msgforFriend = InversionMessage(msgforFriend);
            }
            foreach (SClient sc in list)
            {
                if (sc.uID == uIDfriend)
                {
                    data = Encoding.Unicode.GetBytes(client.uName + ":" + client.uID + ":~PMFORU:" + msgforFriend);
                    sc.stream.Write(data, 0, data.Length);

                    data = Encoding.Unicode.GetBytes(client.uName + ":" + sc.uID + ":~UPM:" + msgforFriend);
                    client.stream.Write(data, 0, data.Length);
                    break;
                }
            }
        }

        public void DeleteFriends(string Msg, List<SClient> list, byte[] data, SClient client)
        {
            string[] words1 = Msg.Split(new char[] { '~' });
            int uIDdeletedFriend = int.Parse(words1[2]);

            data = Encoding.Unicode.GetBytes(client.uID + "~UDELETED");
            foreach (SClient sc in list)
            {
                if (sc.uID == uIDdeletedFriend)
                {
                    sc.stream.Write(data, 0, data.Length);
                    break;
                }
            }
        }
        
        public void FindFriend(string Msg, List<SClient> list, byte[] data, SClient client)
        {
            string[] words2 = Msg.Split(new char[] { '~' });
            int uID = int.Parse(words2[2]);

            bool find = false;
            foreach (SClient sc in list)
            {
                if (sc.uID == uID)
                {
                    data = Encoding.Unicode.GetBytes(client.uName + ":~FIND:" + client.uID);
                    sc.stream.Write(data, 0, data.Length);
                    data = Encoding.Unicode.GetBytes(sc.uName + ":~FIND:" + sc.uID);
                    client.stream.Write(data, 0, data.Length);
                    find = true;
                    break;
                }
            }
            if (find == false)
            {
                data = Encoding.Unicode.GetBytes("~NOTFIND");
                client.stream.Write(data, 0, data.Length);
            }
        }
    }
}