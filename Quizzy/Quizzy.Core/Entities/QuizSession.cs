using Quizzy.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Entities
{
    public class QuizSession
    {

        public int Id { get; set; }

        public string GamePin { get; set; }

        public QuizState State { get; set; } = QuizState.Lobby;

        public int QuizHostId { get; set; }
        public UserAccount QuizHost { get; set; }

        public int QuizId { get; set; }
        public Quiz Quiz { get; set; }

        public ICollection<QuizPlayer> Players { get; set; } = new List<QuizPlayer>();

    }
}
