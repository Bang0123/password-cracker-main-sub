using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MasterCrack
{
    public class Master
    {
        public TcpListener MasterServer { get; set; }
        public IPEndPoint EndPoint { get; set; }
        public List<TcpClient> ConnectClients { get; set; }
        public Master()
        {
            EndPoint = new IPEndPoint(IPAddress.Any, 5678);
            MasterServer = new TcpListener(EndPoint);
            ConnectClients = new List<TcpClient>();
            Console.WriteLine("Server created");
        }

        public void Invoke()
        {
            MasterServer.Start();
            Task.Run(() => { AcceptClients(); });
            Console.WriteLine("Server socket started");

            // TODO Make master give slaves work

        }

        public void AcceptClients()
        {
            while (true)
            {
                var client = MasterServer.AcceptTcpClient();
                if (client.Connected)
                {
                    ConnectClients.Add(client);
                    Console.WriteLine("Client added and is now awaiting work: " + client.GetHashCode());
                }
            }
        }

    }
}