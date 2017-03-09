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
        public string MastersIp { get; set; } = "192.168.1.8";
        public IList<UserInfo> UserInfosList { get; set; }
        public ICollection<string> DictionaryList { get; set; }
        public List<FullUser> Results { get; set; }
        public void BeginWork()
        {
            // TODO Sync errors
            // TODO Test How to avoid double work
            // TODO yea
            try
            {
                TcpClient tcpClient = new TcpClient(MastersIp, 6789);
                Stream stream = tcpClient.GetStream();
                Results = new List<FullUser>();
                StreamWriter sw = new StreamWriter(stream);
                StreamReader sr = new StreamReader(stream);
                sw.AutoFlush = true;
                UserInfosList = GetPasswords(sr, sw);
                Console.WriteLine("Getting passwords");

                while (true)
                {
                    Console.WriteLine("Getting work to do");
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
                // TODO Handle the shutdown the server sends
                Console.WriteLine(e);
            }
        }

        private void SendResult(StreamWriter sw, CrackResults cr)
        {
            string serializedobj = JsonConvert.SerializeObject(cr);
            sw.WriteLine(serializedobj);
            sw.WriteLine("EndOfFile");
            sw.Flush();
        }
        private ICollection<string> GetWorkStarted(StreamReader sr, StreamWriter sw)
        {
            sw.WriteLine("getdc");
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
            ICollection<string> list = JsonConvert.DeserializeObject<ICollection<string>>(receivedstring);
            return list;
        }

        private IList<UserInfo> GetPasswords(StreamReader sr, StreamWriter sw)
        {
            string receivedstring = "";
            sw.WriteLine("getpw");
            while (true)
            {
                string incomingString = sr.ReadLine();
                if (incomingString == "EndOfFile")
                {
                    break;
                }
                receivedstring += incomingString;
            }
            IList<UserInfo> list = JsonConvert.DeserializeObject<IList<UserInfo>>(receivedstring);
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
        private ICollection<FullUser> CheckWordWithVariations(string dictionaryEntry, IList<UserInfo> userInfos)
        {
            List<FullUser> result = new List<FullUser>();

            String possiblePassword = dictionaryEntry;
            ICollection<FullUser> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            String possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            ICollection<FullUser> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            String possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            ICollection<FullUser> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            String possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            ICollection<FullUser> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                ICollection<FullUser> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                ICollection<FullUser> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    ICollection<FullUser> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
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
        private ICollection<FullUser> CheckSingleWord(IList<UserInfo> userInfos, string possiblePassword)
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
            if (results.Count != 0)
            {
                foreach (var fullUser in results)
                {
                    for(int i = 0; i < userInfos.Count; i++)
                    {
                        if (fullUser.Username == userInfos[i].Username)
                        {
                            userInfos.Remove(userInfos[i]);
                        }
                    }
                }
            }
            return results;
        }
    }
}