using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public record QuestionSummaryDto(int correctIndex, int[] optionCounts, (string Name, int Score)[] leaderboard);
}
