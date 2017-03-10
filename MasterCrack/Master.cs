using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MasterCrack.Model;
using MasterCrack.Util;

namespace MasterCrack
{
    public class Master
    {
        public int Indexer { get; set; }
        public string DicPath { get; } = "webster-dictionary.txt";
        public string FilePath { get; } = "passwords.txt";
        public int Eport { get; } = 6789;
        public int IncreaseIndex { get; } = 10000;
        public TcpListener MasterServer { get; }
        public IPEndPoint EndPoint { get; }
        public List<ConnectionHandler> ConnectClients { get; }
        public List<string> DictionaryList { get; set; }
        public Dictionary<string, FullUser> ResultsList { get; }
        public Dictionary<string, UserInfo> Workload { get; set; }
        public object Locker { get; } = new object();
        public Master()
        {
            EndPoint = new IPEndPoint(IPAddress.Any, Eport);
            MasterServer = new TcpListener(EndPoint);
            ConnectClients = new List<ConnectionHandler>();
            ResultsList = new Dictionary<string, FullUser>();
            Console.WriteLine("Server created");
            Indexer = 0;
        }

        public List<string> GetWorkLoadCallback()
        {
            lock (Locker)
            {
                List<string> list = new List<string>();
                if (Indexer < DictionaryList.Count)
                {
                    for (int i = Indexer; i < Indexer + IncreaseIndex && i < DictionaryList.Count; i++)
                    {
                        list.Add(DictionaryList[i]);
                    }
                    Indexer += IncreaseIndex;
                    return list;
                }
                return null;
            }
        }

        public void Invoke()
        {
            MasterServer.Start();
            Console.WriteLine("Reading the password file");
            Workload = PrepareWorkload(FilePath);
            DictionaryList = PrepareDictionary(DicPath);
            Console.WriteLine("Dictionary completely loaded");
            Task.Run(() => { AcceptClients(); });
            Console.WriteLine("Server socket started");

            LoopTillDone();
        }

        private void LoopTillDone()
        {
            while (true)
            {
                if (Workload.Count < 1 || DictionaryList.Count < Indexer)
                {
                    TellClientsShutdown();
                    PrintResults();
                    // TODO kill thread factory process
                    break;
                }
                Thread.Sleep(100);
            }
        }

        private void PrintResults()
        {
            foreach (var result in ResultsList)
            {
                Console.WriteLine(result.Value.PrintUser());
            }
        }

        private void TellClientsShutdown()
        {
            MasterServer.Stop();
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
        private static Dictionary<string, UserInfo> PrepareWorkload(string path)
        {
            var pwList = PasswordFileHandler.ReadPasswordFile(path);
            if (pwList == null || pwList.Count < 1)
            {
                return null;
            }
            return pwList.ToDictionary(userInfo => userInfo.Username);
        }

        private void AcceptClients()
        {
            while (true)
            {
                var client = MasterServer.AcceptTcpClient();
                if (client.Connected)
                {
                    var ch = new ConnectionHandler(client, this);
                    Task.Factory.StartNew(() =>
                    {
                        ch.HandleConnection();
                    });
                    ConnectClients.Add(ch);
                    Console.WriteLine("Client added and is now awaiting work: " + client.GetHashCode());
                }
            }
        }

        public void ResultsCallback(CrackResults results)
        {
            lock (Locker)
            {
                if (results.Results.Count < 1)
                {
                    return; 
                }
                foreach (var user in results.Results)
                {
                    if (!ResultsList.ContainsKey(user.Username))
                    {
                        ResultsList.Add(user.Username, user);
                    }
                    if (Workload.ContainsKey(user.Username))
                    {
                        Workload.Remove(user.Username);
                    }
                }
            }
        }

    } 
}