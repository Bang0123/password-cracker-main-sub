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
        public TcpListener MasterServer { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public List<TcpClient> ConnectClients { get; set; }
        public string FilePath { get; set; } = "Passwords.txt";

        public Master()
        {
            EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5678);
            MasterServer = new TcpListener(EndPoint);
            ConnectClients = new List<TcpClient>();
            Console.WriteLine("Server created");
        }

        public void Invoke()
        {
            MasterServer.Start();
            Task.Run(() => { AcceptClients(); });
            Console.WriteLine("Server socket started");
            Console.WriteLine("Now accepting and handling is threaded");

            // TODO Make master give slaves work

            List<UserInfo> workload = PrepareWorkload(FilePath);


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
                        var ch = new ConnectionHandler(client);
                        ch.HandleConnection();
                    });
                    ConnectClients.Add(client);
                    Console.WriteLine("Client added and is now awaiting work: " + client.GetHashCode());
                }
            }
        }

    }
}