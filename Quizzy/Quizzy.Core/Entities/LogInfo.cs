using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class LogInfo
    {

        public int Id { get; set; }

        public int QuizSessionId { get; set; }
        public QuizSession QuizSession { get; set; }

        public int PlayerAnswerId { get; set; }
        public PlayerAnswer PlayerAnswer { get; set; }

        public int TimeTaken { get; set; }

    }
}
