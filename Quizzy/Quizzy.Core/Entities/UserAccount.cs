using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class UserAccount
    {

        public int Id { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }

        public string Email { get; set; }

        public ICollection<QuizSession> QuizSessions { get; set; } = new List<QuizSession>();

    }
}
