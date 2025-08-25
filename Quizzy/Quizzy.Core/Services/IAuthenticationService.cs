using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public interface IAuthenticationService
    {
        public static IAuthenticationService Instance { get; set; }

        public int LoginUser(string username, string password);
        public UserAccount GetUserDetails(int id);
        public int CreateNewUser(string username, string password, string email);
    }
}
