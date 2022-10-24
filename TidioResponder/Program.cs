using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace TidioResponder
{
    class Program
    {
        static WebSocket ppkWS;
        static List<KeyValuePair<int, string>> AutoReplies = new List<KeyValuePair<int, string>>();

        static string AccessKey = "";
        static string DeviceFingerprint = "";
        static string ProjectPrivateKey = "";
        static string ProjectPublicKey = "";
        static string PersonalVisitorID = "";
        static string OperatorID = "";
        static bool MultiThreaded = false;

        static void Main(string[] args)
        {
            string[] Settings = File.ReadAllLines("Settings.txt");

            string TidioReplies = new WebClient().DownloadString(Settings[0].Split('=')[1]);
            AccessKey = Settings[1].Split('=')[1];
            DeviceFingerprint = Settings[2].Split('=')[1];
            ProjectPrivateKey = Settings[3].Split('=')[1];
            ProjectPublicKey = Settings[4].Split('=')[1];
            PersonalVisitorID = Settings[5].Split('=')[1];
            OperatorID = Settings[6].Split('=')[1];
            MultiThreaded = bool.Parse(Settings[7].Split('=')[1]);

            Task.Factory.StartNew(new Action(() =>
            {
                while (true)
                {
                    try
                    {
                        AutoReplies.Clear();

                        foreach (string replyString in Regex.Split(TidioReplies, @"\r?\n|\r"))
                        {
                            string[] replyStringValues = Regex.Split(replyString, "~>");

                            string replyValue = replyStringValues[1];

                            AutoReplies.Add(new KeyValuePair<int, string>(Convert.ToInt32(replyStringValues[0]), replyValue));
                        }
                    }
                    catch { }

                    Thread.Sleep(60000);
                }
            }));

            using (ppkWS = new WebSocket("wss://socket-chat-us4.tidio.co/socket.io/?ppk=xvstg0cxqiowm9pczjbh5chknqpu5vk9&platform=web&EIO=3&transport=websocket"))
            {
                ppkWS.OnMessage += Ws_OnMessage;
                ppkWS.OnClose += PpkWS_OnClose;
                ppkWS.Connect();

                Task.Factory.StartNew(new Action(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(30000);

                        KeepTidioAlive();
                    }
                })).Wait();
            }

            while (true) { Console.ReadLine(); }
        }

        private static void PpkWS_OnClose(object sender, CloseEventArgs e)
        {
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.FriendlyName);
            Environment.Exit(0);
        }

        private static void Ws_OnMessage(object sender, MessageEventArgs e)
        {
            //WriteConsole(e.Data);

            string updatedJson = Regex.Replace(e.Data, @"^([0-9]*)(?=\[|{)", "");

            #region Message Checks

            if (e.Data.Contains("newMessage"))
            {
                string newMessageJson = Regex.Match(updatedJson, @"\[""newMessage"",(.*?})]").Groups[1].Value;

                try
                {
                    TidioMessage newMessage = JsonConvert.DeserializeObject<TidioMessage>(newMessageJson);

                    if (newMessage != null)
                    {
                        if (newMessage.data.type == "operator") { return; }
                        WriteConsole("New tidio message: " + newMessage.data.message.message
                            + " from: " + newMessage.data.visitor_email + ".");

                        if (MultiThreaded) { Task.Factory.StartNew(new Action(() => { AutoRespondNewMessage(newMessage); })); }
                        else { AutoRespondNewMessage(newMessage); }
                    }
                }
                catch { Console.WriteLine("Couldn't parse message. Probably a Tidio log message."); }
            }

            if (e.Data.Contains("connected"))
            {
                ppkWS.Send("420[\"operatorRegister\",{\"accessKey\":\"" + AccessKey + "\",\"version\":2," +
                     "\"device\":\"web\",\"deviceFingerprint\":\"" + DeviceFingerprint + "\",\"projectPrivateKey\"" +
                     ":\"" + ProjectPrivateKey + "\",\"projectPublicKey\":\"" + ProjectPublicKey + "\"}]");

                ppkWS.Send("42[\"conversationStreamJoin\",{\"visitorId\":\"" + PersonalVisitorID + "\"" +
                    ",\"projectPrivateKey\":\"" + ProjectPrivateKey + "\"" +
                    ",\"projectPublicKey\":\"" + ProjectPublicKey + "\"},null]");

                WriteConsole("Connection succesfull");
            }

            #endregion
        }

        private static void AutoRespondNewMessage(TidioMessage newMessage)
        {
            foreach(var reply in AutoReplies)
            {
                string[] replyStringValues = Regex.Split(reply.Value, "->");

                string readtype = replyStringValues[0];
                string readtext = replyStringValues[1];
                string message = replyStringValues[2];

                if(readtype == "contains")
                {
                    if (Regex.Matches(newMessage.data.message.message.ToLower(), readtext).Count > 0)
                    {
                        SendTidioMessage(newMessage.data.visitor_id, message, reply.Key);

                        return;
                    }
                }
                else
                {
                    if (newMessage.data.message.message.ToLower() == readtext)
                    {
                        SendTidioMessage(newMessage.data.visitor_id, message, reply.Key);

                        return;
                    }
                }
            }
        }

        private static void SendTidioMessage(string VisitorID, string Message, int WriteTime)
        {
            WriteConsole("Replying with: " + Message + " and a timeout of: " + WriteTime.ToString() + " seconds.");

            #region Message Hashing

            Stack<string> iStack = new Stack<string>();
            for (int ind = 0; ind < 256; ind++) {
                iStack.Push(Convert.ToString((ind + 256), 16).Substring(1)); }
            string[] i = iStack.ToArray();

            byte[] o = new byte[16];
            Random r = new Random();
            r.NextBytes(o);

            string MessageHash = (i[o[0]] + i[o[1]] + i[o[2]] + i[o[3]] + "-" + i[o[4]] + i[o[5]] + "-" + i[o[6]] + i[o[7]] + "-" + i[o[8]] + i[o[9]] + "-" + i[o[10]] + i[o[11]] + i[o[12]] + i[o[13]] + i[o[14]] + i[o[15]]).ToLower();

            #endregion

            // Typing :)
            for(int writeIndex = 0; writeIndex < WriteTime; writeIndex++)
            {
                string UnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

                ppkWS.Send("42[\"operatorIsTyping\",{\"visitorId\":\"" + VisitorID + "\",\"channel\":\"chat\",\"operatorId\":"
                    + OperatorID + ",\"time\":" + UnixTimestamp + ",\"projectPrivateKey\":\"" + ProjectPrivateKey
                    + "\",\"projectPublicKey\":\"" + ProjectPublicKey + "\"},null]");

                Thread.Sleep(1000);
            }

            // Send message
            ppkWS.Send("42[\"operatorNewMessage\",{\"visitorId\":\"" + VisitorID + "\",\"message\":\"" + Message 
                + "\",\"operatorId\":" + OperatorID + ",\"channel\":\"chat\",\"hash\":\"" 
                + MessageHash + "\",\"projectPrivateKey\":\"" + ProjectPrivateKey 
                + "\",\"projectPublicKey\":\"" + ProjectPublicKey + "\"},null]");
        }

        private static void KeepTidioAlive()
        {
            WriteConsole("Keeping tidio alive, message every [30] seconds.");

            ppkWS.Send("42[\"operatorLastSeenUpdate\",{\"accessKey\":\"" + AccessKey + "\",\"projectPrivateKey\":\"" 
                + ProjectPrivateKey + "\",\"projectPublicKey\":\"" + ProjectPublicKey + "\"},null]");
            ppkWS.Send("2");
        }

        private static void WriteConsole(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss") + "] " + message);
        }
    }
}
