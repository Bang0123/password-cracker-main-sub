using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainCrack.Model
{
    public class FullUser : UserInfo, IUnencryptedPassword
    {
        public string UnencryptedPassword { get; set; }
        public bool HasUnencryptedPassword()
        {
            if (!String.IsNullOrWhiteSpace(UnencryptedPassword))
            {
                return true;
            }
            return false;
        }

        public FullUser(string username, string password)
        {
            if (String.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException("username");
            }
            if (String.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentNullException("password");
            }
            Username = username;
            UnencryptedPassword = password;
        }

        public FullUser(UserInfo user, string crackedpwd)
        {
            if (String.IsNullOrWhiteSpace(user.Username))
            {
                throw new ArgumentNullException("username");
            }
            if (String.IsNullOrWhiteSpace(user.EntryptedPasswordBase64))
            {
                throw new ArgumentNullException("password");
            }
            Username = user.Username;
            EntryptedPasswordBase64 = user.EntryptedPasswordBase64;
            EntryptedPassword = user.EntryptedPassword;
            UnencryptedPassword = crackedpwd;
        }
        public FullUser(UserInfo user)
        {
            if (String.IsNullOrWhiteSpace(user.Username))
            {
                throw new ArgumentNullException("username");
            }
            if (String.IsNullOrWhiteSpace(user.EntryptedPasswordBase64))
            {
                throw new ArgumentNullException("password");
            }
            Username = user.Username;
            EntryptedPasswordBase64 = user.EntryptedPasswordBase64;
            EntryptedPassword = user.EntryptedPassword;
        }

        public FullUser()
        {
            
        }

        public override string PrintUser()
        {
            return Username + ":" + UnencryptedPassword;
        }
    }
}
