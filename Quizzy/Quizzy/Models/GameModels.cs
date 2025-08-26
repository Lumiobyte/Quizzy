using System;
using System.Collections.Generic;
using System.Linq;

namespace Quizzy.Web.Models
{
    public record Player(string ConnectionId, string Name)
    {
        public int Score { get; set; } = 0;
        public bool HasAnswered { get; set; } = false;
        public int? LastAnswer { get; set; }
        public int AnswerTimeMs { get; set; } = 0;
    }

    public record Question(string Text, string[] Options, int CorrectIndex, int DurationSeconds = 20);

    public class Room
    {
        public string RoomId { get; init; } = string.Empty;
        public string HostConnectionId { get; set; } = string.Empty;
        public Dictionary<string, Player> Players { get; } = new();

        public Question? CurrentQuestion { get; private set; }
        public DateTimeOffset? QuestionStartTime { get; private set; }
        public Question? UpcomingQuestion { get; private set; }
        public DateTimeOffset? NextQuestionStartTime { get; private set; }

        public bool QuestionActive => CurrentQuestion != null && QuestionStartTime != null;

        public void ScheduleNextQuestion(Question q, DateTimeOffset startAtUtc)
        {
            UpcomingQuestion = q;
            NextQuestionStartTime = startAtUtc;
        }

        public void StartScheduledQuestionIfDue()
        {
            if (UpcomingQuestion != null && NextQuestionStartTime != null && DateTimeOffset.UtcNow >= NextQuestionStartTime)
            {
                StartQuestion(UpcomingQuestion);
                UpcomingQuestion = null;
                NextQuestionStartTime = null;
            }
        }

        public void StartQuestion(Question q)
        {
            CurrentQuestion = q;
            QuestionStartTime = DateTimeOffset.UtcNow;
            foreach (var p in Players.Values)
            {
                p.HasAnswered = false;
                p.LastAnswer = null;
                p.AnswerTimeMs = 0;
            }
        }

        public void EndQuestion()
        {
            CurrentQuestion = null;
            QuestionStartTime = null;
        }
    }
}