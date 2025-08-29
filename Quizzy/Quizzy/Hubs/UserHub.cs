using Microsoft.AspNetCore.SignalR;

namespace Quizzy.Web.Hubs
{
    public class UserHub : Hub
    {
        public Task SendAnswer(int userID, string answer)
        {
            return Clients.Caller.SendAsync("ReceiveAnswer", userID, answer);
        }

        public Task SendChatMessage(int userID, string message, List<string> clients)
        {
            return Clients.Clients(clients).SendAsync("ReceiveMessage", userID, message);
        }
    }
}
