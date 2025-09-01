using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public record QuestionStartedDto(string Text, string[] Options, int DurationSeconds, DateTimeOffset QuestionStartTimeUtc);
}
