using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public class LeaderboardDto
    {
        public string QuizTitle { get; set; }
        public int TotalQuestions { get; set; }

        public List<LeaderboardPlayerDto> LeaderboardPlayers { get; set; }
    }
}
