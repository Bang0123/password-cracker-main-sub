using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using MasterCrack.Model;
using MasterCrack.Util;

namespace MasterCrack
{
    public class Master
    {
        public string DicPath { get; set; } = "webster-dictionary.txt";
        public TcpListener MasterServer { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public List<TcpClient> ConnectClients { get; set; }
        public List<FullUser> ResultsList { get; set; }
        public List<UserInfo> Workload { get; set; }
        public List<String> DictionaryList { get; set; }
        public string FilePath { get; set; } = "passwords.txt";
        public int Indexer { get; set; }
        public object Locker = new object();
        public Master()
        {
            EndPoint = new IPEndPoint(IPAddress.Any, 6789);
            MasterServer = new TcpListener(EndPoint);
            ConnectClients = new List<TcpClient>();
            ResultsList = new List<FullUser>();
            Console.WriteLine("Server created");
            Indexer = 0;
        }

        public List<String> GiveWorkLoad()
        {
            lock (Locker)
            {
                List<String> list = new List<string>();
                for (int i = Indexer; i < Indexer + 1000 && i < DictionaryList.Count; i++)
                {
                    list.Add(DictionaryList[i]);
                }
                Indexer += 1000;
                return list;
            }

        }

        public void Invoke()
        {
            MasterServer.Start();
            Task.Run(() => { AcceptClients(); });
            Console.WriteLine("Server socket started");
            Console.WriteLine("Now accepting and handling is threaded");

            // TODO Make master give slaves work

            Workload = PrepareWorkload(FilePath);
            DictionaryList = PrepareDictionary(DicPath);

        }



        private List<string> PrepareDictionary(string path)
        {
            List<string> ls = new List<string>();
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader dictionary = new StreamReader(fs))
            {
                while (!dictionary.EndOfStream)
                {
                    ls.Add(dictionary.ReadLine());
                }
            }
            return ls;
        }
        private List<UserInfo> PrepareWorkload(string path)
        {
            return PasswordFileHandler.ReadPasswordFile(path);
        }

        public void AcceptClients()
        {
            while (true)
            {
                var client = MasterServer.AcceptTcpClient();
                if (client.Connected)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var ch = new ConnectionHandler(client, this);
                        ch.HandleConnection();
                    });
                    ConnectClients.Add(client);
                    Console.WriteLine("Client added and is now awaiting work: " + client.GetHashCode());
                }
            }
        }

    }
}