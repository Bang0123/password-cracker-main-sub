using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using MasterCrack.Model;
using MasterCrack.Util;
using Newtonsoft.Json;

namespace SlaveCrack
{

    /// <summary>
    /// Only the master keeps the results ;)
    /// </summary>
    public class Slave
    {
        public TcpClient TcpClient { get; set; }
        public HashAlgorithm HashAlgorithm { get; }
        //public string MastersIp { get; set; } = "127.0.0.1";
        public string MastersIp { get; set; } = "192.168.1.8";
        public int MastersPort { get; } = 6789;
        public IList<UserInfo> UserInfosList { get; set; }
        public IList<string> DictionaryList { get; set; }
        public TimeSpan TimeElapsed { get; set; }
        public List<FullUser> Results { get; set; }

        // TODO Slave kan blive mere async i sine cracking handlinger og what not
        public Slave()
        {
            HashAlgorithm = new SHA1CryptoServiceProvider();
            //_messageDigest = new MD5CryptoServiceProvider();
            Results = new List<FullUser>();
        }

        public void BeginWork()
        {
            try
            {
                TcpClient = new TcpClient(MastersIp, MastersPort);
                Stream stream = TcpClient.GetStream();
                StreamWriter sw = new StreamWriter(stream);
                StreamReader sr = new StreamReader(stream);
                sw.AutoFlush = true;
                UserInfosList = GetPasswords(sr, sw);
                Console.WriteLine("Getting passwords");

                while (true)
                {
                    Console.WriteLine("Getting work to do");
                    DictionaryList = GetWorkStarted(sr, sw);
                    if (DictionaryList == null || DictionaryList.Count < 1)
                    {
                        sw.WriteLine("EndOfDictionary");
                        sw.Flush();
                        Thread.Sleep(100);
                        throw new ApplicationExitException("Server reached end of dictionary");
                    }
                    if (DictionaryList != null && DictionaryList.Count > 0)
                    {
                        Console.WriteLine("Working!");
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        var hashes = 0;
                        foreach (var dictionaryEntry in DictionaryList)
                        {
                            Results.AddRange(CheckWordWithVariations(dictionaryEntry, UserInfosList, ref hashes));
                        }
                        stopwatch.Stop();
                        string total = $"Theres {UserInfosList.Count} passwords left\n{Results.Count} passwords was found\n{DictionaryList.Count} words was tested";
                        Console.WriteLine(total);
                        Console.WriteLine();
                        TimeElapsed = TimeElapsed.Add(stopwatch.Elapsed);
                        string time = $"Time elapsed: {stopwatch.Elapsed}";
                        Console.WriteLine(time);
                        var resultObject = new CrackResults(Results, stopwatch.Elapsed, total, time , hashes);
                        SendResult(sw, resultObject);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ApplicationExitException)
                {
                    Console.WriteLine("Shutdown recieved");
                    Console.WriteLine("Total time slave was working on passwords: " + TimeElapsed);
                    TcpClient.Close();
                }
                else
                {
                    Console.WriteLine(e);
                }
            }
            
        }

        private void SendResult(StreamWriter sw, CrackResults cr)
        {
            string serializedobj = JsonConvert.SerializeObject(cr);
            sw.WriteLine(serializedobj);
            sw.WriteLine("EndOfFile");
            sw.Flush();
            Results.Clear();
        }
        private IList<string> GetWorkStarted(StreamReader sr, StreamWriter sw)
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
            IList<string> list = JsonConvert.DeserializeObject<List<string>>(receivedstring);
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
            IList<UserInfo> list = JsonConvert.DeserializeObject<List<UserInfo>>(receivedstring);
            return list;
        }

        /// <summary>
        /// Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <param name="hashes">passed by ref to change the value</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private ICollection<FullUser> CheckWordWithVariations(string dictionaryEntry, IList<UserInfo> userInfos, ref int hashes)
        {
            List<FullUser> result = new List<FullUser>();

            string possiblePassword = dictionaryEntry;
            IList<FullUser> partialResult = CheckSingleWord(userInfos, possiblePassword, ref hashes);
            result.AddRange(partialResult);

            string possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            IList<FullUser> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase, ref hashes);
            result.AddRange(partialResultUpperCase);

            string possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            IList<FullUser> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized, ref hashes);
            result.AddRange(partialResultCapitalized);

            string possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            IList<FullUser> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse, ref hashes);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                string possiblePasswordEndDigit = dictionaryEntry + i;
                IList<FullUser> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit, ref hashes);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                string possiblePasswordStartDigit = i + dictionaryEntry;
                IList<FullUser> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit, ref hashes);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    string possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    IList<FullUser> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit, ref hashes);
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
        /// <param name="hashes">Passed by ref to change the value</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IList<FullUser> CheckSingleWord(IList<UserInfo> userInfos, string possiblePassword, ref int hashes)
        {
            char[] charArray = possiblePassword.ToCharArray();
            byte[] passwordAsBytes = Array.ConvertAll(charArray, PasswordFileHandler.GetConverter());
            byte[] encryptedPassword = HashAlgorithm.ComputeHash(passwordAsBytes);
            hashes++;
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            List<FullUser> results = new List<FullUser>();
            foreach (UserInfo userInfo in userInfos)
            {
                if (CompareBytes(userInfo.EntryptedPassword, encryptedPassword))
                {
                    results.Add(new FullUser(userInfo, possiblePassword));
                    Console.WriteLine(userInfo.Username + " " + possiblePassword);
                }
            }
            FilterWhatsDone(userInfos, results);
            return results;
        }

        private void FilterWhatsDone(IList<UserInfo> userInfos, List<FullUser> results)
        {
            if (results.Count != 0)
            {
                foreach (var fullUser in results)
                {
                    for (int i = 0; i < userInfos.Count; i++)
                    {
                        if (fullUser.Username == userInfos[i].Username)
                        {
                            userInfos.Remove(userInfos[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Compares to byte arrays. Encrypted words are byte arrays
        /// </summary>
        /// <param name="firstArray"></param>
        /// <param name="secondArray"></param>
        /// <returns></returns>
        public static bool CompareBytes(IList<byte> firstArray, IList<byte> secondArray)
        {
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("firstArray");
            //}
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("secondArray");
            //}
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }
            for (int i = 0; i < firstArray.Count; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }
    }
}