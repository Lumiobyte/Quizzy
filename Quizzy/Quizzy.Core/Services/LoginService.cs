using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System.Net.Mail;

namespace Quizzy.Core.Services
{
    public class LoginService(IUnitOfWork repository) : ILoginService
    {
        public async Task<Guid?> LoginUser(string username, string password)
        {
            var users = await repository.UserAccounts.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Username == username && u.Password == password);
            if (user == null) return null;
            return user.Id;
        }

        public UserAccount GetUserDetails(int id)
        {
            return new UserAccount();
        }

        public async Task<Guid> CreateNewUser(string username, string password, string email)
        {
            ValidateDetails(username, password, email);
            var id = Guid.NewGuid();
            await repository.UserAccounts.AddAsync(new UserAccount
            {
                Id = id,
                Username = username,
                Password = password,
                Email = email
            });
            await repository.SaveChangesAsync();
            return id;
        }

        void ValidateDetails(string username, string password, string email)
        {
            var users = repository.UserAccounts.GetAllAsync().Result;
            if (users.Any(u => u.Username == username)) throw new ArgumentException("Username already exists");
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username cannot be empty");
            if (string.IsNullOrWhiteSpace(password) || password == "empty") throw new ArgumentException("Password cannot be empty"); // The password cannot be "empty"
            try { new MailAddress(email); }
            catch { throw new ArgumentException("Email is not valid"); }
        }
    }
}
