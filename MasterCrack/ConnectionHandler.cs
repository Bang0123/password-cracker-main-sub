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

namespace MasterCrack
{
    public class ConnectionHandler
    {

        private TcpClient connectionSocket;

        public ConnectionHandler(TcpClient tcpClient)
        {
            connectionSocket = tcpClient;
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
                string answer = "";

                while (!string.IsNullOrEmpty(message))
                {
                    if (message.StartsWith("GET"))
                    {
                        



                        sw.WriteLine(answer);
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
