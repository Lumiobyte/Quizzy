using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public class LeaderboardPlayerDto
    {
        public string PlayerName { get; }
        public int PlayerRanking { get; }
        public int PlayerScore { get; }
    }
}
