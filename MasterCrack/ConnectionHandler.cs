using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MasterCrack
{
    public class ConnectionHandler
    {
        public Master MyMaster { get; set; }
        private TcpClient connectionSocket;
        

        public ConnectionHandler(TcpClient tcpClient)
        {
            connectionSocket = tcpClient;
        }
        public ConnectionHandler(TcpClient tcpClient, Master master)
        {
            connectionSocket = tcpClient;
            MyMaster = master;
        }

        public void HandleConnection()
        {
            Stream ns = connectionSocket.GetStream();
            try
            {
                StreamReader sr = new StreamReader(ns);
                StreamWriter sw = new StreamWriter(ns);
                sw.AutoFlush = true;
                string message = sr.ReadLine();

                while (!string.IsNullOrEmpty(message))
                {
                    if (message.StartsWith("getpw"))
                    {
                        string passWordS = JsonConvert.SerializeObject(MyMaster.Workload);
                        sw.WriteLine(passWordS);
                        sw.WriteLine("EndOfFile");
                    }
                    if (message.StartsWith("getdc"))
                    {
                        string listString = JsonConvert.SerializeObject(MyMaster.GiveWorkLoad());
                        sw.WriteLine(listString);
                        sw.WriteLine("EndOfFile");
                        
                    }
                    message = sr.ReadLine();
                    Console.WriteLine("Client: " + message);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Client DC: " + this.GetHashCode());
            }
            ns.Close();
            connectionSocket.Close();
        }
    }
}
