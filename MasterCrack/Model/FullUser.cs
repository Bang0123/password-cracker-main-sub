using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterCrack.Model
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

        public FullUser()
        {
            
        }

        public override string PrintUser()
        {
            return Username + ":" + UnencryptedPassword;
        }
    }
}
