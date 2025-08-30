using Quizzy.Core.Entities;

namespace Quizzy.Core.Services
{
    public interface IEmailService
    {
        public Task SendEmailAsync(UserAccount reciever, string subject, string body, string[]? attachments = null); // Don't await because its slow :(
    }
}
