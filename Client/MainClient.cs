using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using WebSocketSharp;
namespace Client
{
    class Program
    {

        static string GUID;
        static WebSocket Ws;
        static string Name;
        static void Main(string[] args)
        {
            baştan:
            Console.Write("Name: ");
            string input1 = Console.ReadLine();
            if (input1.Length > 5 && input1.Length < 15)
            {
                Name = input1;
            }
            else
            {
                Console.WriteLine("Name should be bigger than 5 characters and smaller then 15 characters.");
                goto baştan;
            }
            Ws = new WebSocket("ws://192.168.1.29/Chat");
            Ws.OnMessage += Ws_OnMessage;
            Ws.Connect();
            Ws.OnClose += Ws_OnClose;
            Console.WriteLine("Connected to the server.");
            Console.WriteLine("");
            while (true)
            {
                string input = Console.ReadLine();
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("command", "message");
                dic.Add("message", input);
                dic.Add("guid", GUID);
                Ws.Send(ObjectToByteArray(dic));
            }
            
        }

        private static void Ws_OnClose(object sender, CloseEventArgs e)
        {
            Environment.Exit(0);
        }

        private static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            CommandManager(e.RawData);
        }

        static void CommandManager(byte[] data)
        {
            Dictionary<string, string> dic = ByteArrayToDictionary(data);
            string command;
            string from;
            string message;
            dic.TryGetValue("command", out command);
            switch (command)
            {
                default:
                    break;
                case "setguid":
                    dic.TryGetValue("guid", out GUID);
                    Dictionary<string, string> setname = new Dictionary<string, string>();
                    setname.Add("command", "setname");
                    setname.Add("guid", GUID);
                    setname.Add("name", Name);
                    Console.WriteLine("Your guid: " + GUID);
                    Ws.Send(ObjectToByteArray(setname));
                    break;
                case "message":
                    dic.TryGetValue("from", out from);
                    dic.TryGetValue("message", out message);
                    Console.WriteLine(from + ": " + message);
                    break;
            }
        }

        static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        static Dictionary<string, string> ByteArrayToDictionary(byte[] bytes)
        {
            if (bytes.Length > 0)
            {
                BinaryFormatter bf = new BinaryFormatter();
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(bytes);
                    ms.Position = 0;
                    return (Dictionary<string, string>)bf.Deserialize(ms);
                }
            }
            return null;
        }
    }
}
