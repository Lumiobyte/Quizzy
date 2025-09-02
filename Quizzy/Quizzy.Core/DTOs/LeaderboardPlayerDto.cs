using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public class LeaderboardPlayerDto
    {
        public string PlayerName { get; set; }
        public int PlayerRanking { get; set;  }
        public int PlayerScore { get; set;  }
    }
}
