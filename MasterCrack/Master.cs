using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string[] DicPath { get; } = { "webster-dictionary.txt", "webster-dictionary-reduced.txt" };
        public string FilePath { get; } = "passwords.txt";
        public int Eport { get; } = 6789;
        public int IncreaseIndex { get; set; } = 10000;
        public TcpListener MasterServer { get; }
        public IPEndPoint EndPoint { get; }
        public List<ConnectionHandler> ConnectClients { get; }
        public List<string> DictionaryList { get; set; }
        public Dictionary<string, FullUser> ResultsList { get; }
        public Dictionary<string, UserInfo> Workload { get; private set; }
        public object Locker { get; } = new object();
        public Stopwatch TotalTimeWatch { get; set; }
        public TimeSpan ClientsWorkTimeSpan { get; set; }
        public int Hashestried { get; set; }
        public bool DoneBool { get; set; }
        public bool EndOfDictionary { get; set; }

        public Master()
        {
            EndPoint = new IPEndPoint(IPAddress.Any, Eport);
            MasterServer = new TcpListener(EndPoint);
            ConnectClients = new List<ConnectionHandler>();
            ResultsList = new Dictionary<string, FullUser>();
            Console.WriteLine("Server created");
            ClientsWorkTimeSpan = TimeSpan.Zero;
            Indexer = 0;
            Hashestried = 0;
        }

        public List<string> GetWorkLoadCallback()
        {
            lock (Locker)
            {
                if (EndOfDictionary)
                {
                    return null;
                }
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
            TotalTimeWatch = Stopwatch.StartNew();
            MasterServer.Start();
            Console.WriteLine("Reading the password file");
            Workload = PreparePasswords(FilePath);
            // TODO More dictionaries?
            DictionaryList = PrepareDictionary(DicPath[0]);
            Console.WriteLine("Dictionary completely loaded");
            Task.Run(() => { AcceptClients(); });
            Console.WriteLine("Server socket started");

            LoopTillDone();
        }

        private void LoopTillDone()
        {
            while (true)
            {
                Console.Clear();
                PrintResults();
                if (IsDone())
                {
                    TotalTimeWatch.Stop();
                    PrintResults();
                    TellClientsShutdown();
                    break;
                }
                Thread.Sleep(250);
            }
        }

        private bool IsDone()
        {
            return DoneBool;
        }

        private void PrintResults()
        {
            foreach (var result in ResultsList)
            {
                Console.WriteLine(result.Value.PrintUser());
            }
            PrintTimeElapsed();
        }

        private void PrintTimeElapsed()
        {
            Console.WriteLine("Hashes computed and tried: " + Hashestried);
            Console.WriteLine("Total time used by clients added alltogether: " + ClientsWorkTimeSpan);
            Console.WriteLine("Total time Elapsed: " + TotalTimeWatch?.Elapsed);
        }

        private void TellClientsShutdown()
        {
            foreach (var ch in ConnectClients)
            {
                if (!ch.IsWorking())
                {
                    ch.Dispose();
                }
            }
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
                if (ls.Count < 10000)
                {
                    IncreaseIndex = 1000;
                }
            }
            return ls;
        }
        private Dictionary<string, UserInfo> PreparePasswords(string path)
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
            while (!IsDone())
            {
                try
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
                catch (Exception)
                {
                    Console.WriteLine("Reached the end of dictionary");
                }

            }
        }

        public void ResultsCallback(CrackResults results)
        {
            lock (Locker)
            {
                Hashestried += results.Hashes;
                ClientsWorkTimeSpan = ClientsWorkTimeSpan.Add(results.TimeElapsed);
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

        public void ShutdownCallback(ConnectionHandler caller)
        {
            lock (Locker)
            {
                if (DoneBool)
                {
                    return;
                }
                EndOfDictionary = true;
                ConnectClients.Remove(caller);
                if (ConnectClients.Count == 0)
                {
                    DoneBool = true;
                }
            }

        }
    }
}
