using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        static IAuthenticationService? instance = null;
        public static IAuthenticationService Instance
        {
            get
            {
                if (instance == null) instance = new AuthenticationService();
                return instance;
            }
            set
            {
                instance = value;
            }
        }

        public int LoginUser(string username, string password)
        {
            return 1;
        }

        public UserAccount GetUserDetails(int id)
        {
            return new UserAccount();
        }

        public int CreateNewUser(string username, string password, string email)
        {
            ValidateDetails(username, password, email);
            // Get new id
            // Save to db
            return 1;
        }

        void ValidateDetails(string username, string password, string email)
        {
            // Check that username is unique
            // Check that email is unique
            try { new MailAddress(email); }
            catch { throw new ArgumentException("Email is not valid"); }
        }
    }
}
