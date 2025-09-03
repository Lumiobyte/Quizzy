using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.DTOs
{
    public class SessionStateDto
    {
        public string SessionId { get; init; } = string.Empty;

        public string? Host { get; init; }

        public object[] Players { get; init; } = Array.Empty<object>();

        public QuestionBlock? Question { get; init; }

        public UpcomingBlock? Upcoming { get; init; }

        public bool Finished { get; init; }

        public record QuestionBlock(string Text, string[] Options, int DurationSeconds, DateTimeOffset QuestionStartTimeUtc);

        public record UpcomingBlock(string Text, string[] Options, DateTimeOffset NextQuestionStartTimeUtc);
    }
}
