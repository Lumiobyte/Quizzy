using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Services
{
    public interface ILoginService
    {
        public Task<Guid?> LoginUser(string username, string password);
        public UserAccount GetUserDetails(int id);
        public Task<Guid> CreateNewUser(string username, string password, string email);
    }
}
