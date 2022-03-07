using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using MainCrack.Model;

namespace MainCrack.Util
{
    public class PasswordFileHandler
    {
        private static readonly Converter<char, byte> Converter = CharToByte;

        /// <summary>
        /// With this method you can make you own password file
        /// </summary>
        /// <param name="filename">Name of password file</param>
        /// <param name="usernames">List of usernames</param>
        /// <param name="passwords">List of passwords in clear text</param>
        /// <exception cref="ArgumentException">if usernames and passwords have different lengths</exception>
        public static void WritePasswordFile(String filename, String[] usernames, String[] passwords)
        {
            HashAlgorithm messageDigest = new SHA1CryptoServiceProvider();
            if (usernames.Length != passwords.Length)
            {
                throw new ArgumentException("usernames and passwords must be same lengths");
            }
            using (FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                for (int i = 0; i < usernames.Length; i++)
                {
                    byte[] passwordAsBytes = Array.ConvertAll(passwords[i].ToCharArray(), GetConverter());
                    byte[] encryptedPassword = messageDigest.ComputeHash(passwordAsBytes);
                    String line = usernames[i] + ":" + Convert.ToBase64String(encryptedPassword) + "\n";
                    sw.WriteLine(line);
                }
            }
        }

        /// <summary>
        /// Reads all the username + encrypted password from the password file
        /// </summary>
        /// <param name="filename">the name of the password file</param>
        /// <returns>A list of (username, encrypted password) pairs</returns>
        public static List<UserInfo> ReadPasswordFile(String filename)
        {
            List<UserInfo> result = new List<UserInfo>();

            FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using (StreamReader sr = new StreamReader(fs))
            {

                while (!sr.EndOfStream)
                {
                    String line = sr.ReadLine();
                    String[] parts = line.Split(":".ToCharArray());
                    UserInfo userInfo = new UserInfo(parts[0], parts[1]);
                    result.Add(userInfo);
                }
                return result;
            }
        }

        public static Converter<char, byte> GetConverter()
        {
            return Converter;
        }

        /// <summary>
        /// Converting a char to a byte can be done in many ways.
        /// This is one way ...
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        private static byte CharToByte(char ch)
        {
            return Convert.ToByte(ch);
        }
    }
}