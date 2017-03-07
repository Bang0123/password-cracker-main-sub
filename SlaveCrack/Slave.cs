using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using MasterCrack.Model;
using MasterCrack.Util;
using Newtonsoft.Json;

namespace SlaveCrack
{
    public class Slave
    {
        public TcpClient tcpClient { get; set; }
        public HashAlgorithm HashAlgorithm { get; }
        public string Ip { get; set; } = "127.0.0.1";
        public List<UserInfo> UserInfosList { get; set; }
        public List<string> DictionaryList { get; set; }
        public List<FullUser> Results { get; set; }
        public void BeginWork()
        {
            // TODO Connect this to master
            // TODO MAKE this accept tcp work from master
            try
            {
                TcpClient tcpClient = new TcpClient(Ip, 5678);
                Stream stream = tcpClient.GetStream();
                Results = new List<FullUser>();
                StreamWriter sw = new StreamWriter(stream);
                StreamReader sr = new StreamReader(stream);
                sw.AutoFlush = true;
                UserInfosList = GetPasswords(sr, sw);

                while (true)
                {
                    DictionaryList = GetWorkStarted(sr, sw);
                    if (DictionaryList != null || DictionaryList.Count != 0)
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        foreach (var dictionaryEntry in DictionaryList)
                        {
                            Results.AddRange(CheckWordWithVariations(dictionaryEntry, UserInfosList));
                        }
                        stopwatch.Stop();
                        //Console.WriteLine(string.Join(", ", Results));
                        string total = $"Out of {UserInfosList.Count} password {Results.Count} was found ";
                        Console.WriteLine(total);
                        Console.WriteLine();
                        string time = $"Time elapsed: {stopwatch.Elapsed}";
                        Console.WriteLine(time);
                        var resultObject = new CrackResults(Results, stopwatch.Elapsed, total, time);
                        SendResult(sw, resultObject);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void SendResult(StreamWriter sw, CrackResults cr)
        {
            string serializedobj = JsonConvert.SerializeObject(cr);
            sw.WriteLine(serializedobj);
            sw.WriteLine("EndOfFile");
        }
        private List<string> GetWorkStarted(StreamReader sr, StreamWriter sw)
        {
            sw.WriteLine("getworkstarted");
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
            List<string> list = JsonConvert.DeserializeObject<List<string>>(receivedstring);
            return list;
        }

        private List<UserInfo> GetPasswords(StreamReader sr, StreamWriter sw)
        {
            string receivedstring = "";
            sw.WriteLine("getpasswordslist");
            while (true)
            {
                string incomingString = sr.ReadLine();
                if (incomingString == "EndOfFile")
                {
                    break;
                }
                receivedstring += incomingString;
            }
            List<UserInfo> list = JsonConvert.DeserializeObject<List<UserInfo>>(receivedstring);
            return list;
        }


        public Slave()
        {
            HashAlgorithm = new SHA1CryptoServiceProvider();
            //_messageDigest = new MD5CryptoServiceProvider();
        }

        /// <summary>
        /// Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<FullUser> CheckWordWithVariations(String dictionaryEntry, List<UserInfo> userInfos)
        {
            List<FullUser> result = new List<FullUser>();

            String possiblePassword = dictionaryEntry;
            IEnumerable<FullUser> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            String possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            IEnumerable<FullUser> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            String possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            IEnumerable<FullUser> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            String possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            IEnumerable<FullUser> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                IEnumerable<FullUser> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                IEnumerable<FullUser> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    IEnumerable<FullUser> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result.AddRange(partialResultStartEndDigit);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks a single word (or rather a variation of a word): Encrypts and compares to all entries in the password file
        /// </summary>
        /// <param name="userInfos"></param>
        /// <param name="possiblePassword">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<FullUser> CheckSingleWord(IEnumerable<UserInfo> userInfos, String possiblePassword)
        {
            char[] charArray = possiblePassword.ToCharArray();
            byte[] passwordAsBytes = Array.ConvertAll(charArray, PasswordFileHandler.GetConverter());
            byte[] encryptedPassword = HashAlgorithm.ComputeHash(passwordAsBytes);
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            List<FullUser> results = new List<FullUser>();
            foreach (UserInfo userInfo in userInfos)
            {
                if (PasswordUtils.CompareBytes(userInfo.EntryptedPassword, encryptedPassword))
                {
                    results.Add(new FullUser(userInfo, possiblePassword));
                    Console.WriteLine(userInfo.Username + " " + possiblePassword);
                }
            }
            return results;
        }
    }
}