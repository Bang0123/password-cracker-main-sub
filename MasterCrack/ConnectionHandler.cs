using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using MasterCrack.Model;
using Newtonsoft.Json;

namespace MasterCrack
{
    public class ConnectionHandler
    {
        public Master MyMaster { get; set; }
        private bool _workload;
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
                        string passWordS = JsonConvert.SerializeObject(MyMaster.Workload.Select(kvp => kvp.Value).ToList());
                        sw.WriteLine(passWordS);
                        sw.WriteLine("EndOfFile");
                    }
                    if (message.StartsWith("getdc"))
                    {
                        string listString = JsonConvert.SerializeObject(MyMaster.GetWorkLoadCallback());
                        sw.WriteLine(listString);
                        sw.WriteLine("EndOfFile");
                        _workload = true;
                    }
                    if (_workload)
                    {
                        string receivedstring = "";
                        while (true)
                        {
                            string incomingString = sr.ReadLine();
                            if (incomingString == "EndOfFile")
                            {
                                break;
                            }
                            receivedstring += incomingString;
                        }
                        CrackResults results = JsonConvert.DeserializeObject<CrackResults>(receivedstring);
                        _workload = false;

                        MyMaster.ResultsCallback(results);

                        Console.WriteLine("Client has finished its current work: " + connectionSocket.GetHashCode());
                        Console.WriteLine(results.TotalsString);
                        Console.WriteLine(results.TimeString);
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
