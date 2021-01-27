using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Server
{
    class Program : WebSocketBehavior
    {
        
        static WebSocketServer Ws;
        static List<User> Users = new List<User>();

        static void Main(string[] args)
        {
            Console.WriteLine("Server starting....");
            Ws = new WebSocketServer("ws://localhost");
            Ws.AddWebSocketService<Program>("/Chat");
            Ws.Start();
            Console.WriteLine("Websocket server started on localhost/Chat");
            while (true)
            {
                Console.WriteLine("Type DONE to close server.");
                string input = Console.ReadLine();
                if (input.ToLower() == "done")
                {
                    Ws.Stop();
                    break;
                }
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            CommandManager(e.RawData, Context.WebSocket);
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

        static Dictionary<string,string> ByteArrayToDictionary(byte[] bytes)
        {
            try
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
            catch (Exception)
            {
                return null;
            }
        }
        
        static void CommandManager(byte[] data, WebSocket ws)
        {
            Dictionary<string, string> dic = ByteArrayToDictionary(data);
            string command = null;
            string name;
            string guid;
            string message;
            dic.TryGetValue("command", out command);
            if (command != null)
            {
                switch (command)
                {
                    default:
                        ws.Close();
                        break;
                    case "setname":
                        dic.TryGetValue("name", out name);
                        dic.TryGetValue("guid", out guid);
                        if (name.Length > 5 && name.Length < 15)
                        {
                            foreach (var item in Users)
                            {
                                if (item.GUID == guid)
                                {
                                    item.Name = name;
                                    item.CanText = true;
                                    Console.WriteLine("New user connected with GUID: " + guid + " Name: " + name);
                                    break;
                                }
                            }
                        }
                        break;
                    case "message":
                        User user = null;
                        dic.TryGetValue("guid", out guid);
                        dic.TryGetValue("message", out message);
                        foreach (var item in Users)
                        {
                            if (item.GUID == guid)
                            {
                                user = item;
                            }
                        }
                        if (user != null)
                        {
                            if (user.CanText)
                            {
                                WebSocketServiceHost host;
                                Ws.WebSocketServices.TryGetServiceHost("/Chat", out host);
                                Dictionary<string, string> dicmessage = new Dictionary<string, string>();
                                dicmessage.Add("command", "message");
                                dicmessage.Add("from", user.Name);
                                dicmessage.Add("message", message);
                                host.Sessions.Broadcast(ObjectToByteArray(dicmessage));
                            }
                        }

                        break;
                }
            }
            else
            {
                ws.Close();
            }
        }

        protected override void OnOpen()
        {
            string guid = Guid.NewGuid().ToString();
            WebSocket ws = Context.WebSocket;
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("command", "setguid");
            dic.Add("guid", guid);
            ws.Send(ObjectToByteArray(dic));
            Users.Add(new User(null, guid));
        }
    }

    class User
    {
        public string Name { get; set; }
        public string GUID { get; set; }

        public bool CanText { get; set; }

        public User(string name, string guid)
        {
            Name = name;
            GUID = guid;
            CanText = false;
        }
    }
}
