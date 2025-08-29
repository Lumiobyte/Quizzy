using System;
using System.Linq;

namespace Quizzy.Web.Models
{
    public record QuestionStartedDto(string Text, string[] Options, int DurationSeconds, DateTimeOffset QuestionStartTimeUtc);
    public record QuestionSummaryDto(int correctIndex, int[] optionCounts, (string Name, int Score)[] leaderboard);

    public class SessionStateDto
    {
        public string RoomId { get; init; } = string.Empty;
        public string? Host { get; init; }
        public object[] Players { get; init; } = Array.Empty<object>();
        public QuestionBlock? Question { get; init; }
        public UpcomingBlock? Upcoming { get; init; }

        public record QuestionBlock(string Text, string[] Options, int DurationSeconds, DateTimeOffset QuestionStartTimeUtc);
        public record UpcomingBlock(string Text, string[] Options, DateTimeOffset NextQuestionStartTimeUtc);
    }
}