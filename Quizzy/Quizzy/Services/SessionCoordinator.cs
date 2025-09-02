using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Quizzy.Core.Entities;

namespace Quizzy.Web.Services
{
    public class SessionCoordinator
    {
        private readonly ConcurrentDictionary<string, SessionRuntime> _sessions = new(StringComparer.OrdinalIgnoreCase);

        public SessionRuntime GetOrCreate(string pin, Func<QuizSession> factory) => _sessions.GetOrAdd(pin, _ => new SessionRuntime(factory()));

        public bool TryGet(string pin, out SessionRuntime runtime) => _sessions.TryGetValue(pin, out runtime);

        public void RemoveConnection(string connectionId)
        {
            foreach (var keyValuePair in _sessions)
            {
                var runtime = keyValuePair.Value;
                runtime.PlayerByConnection.TryRemove(connectionId, out _);
                if (runtime.HostConnectionId == connectionId) runtime.HostConnectionId = string.Empty;
            }
        }
    }

    public class SessionRuntime
    {
        public QuizSession Session { get; }

        public string HostConnectionId { get; set; } = string.Empty;

        public ConcurrentDictionary<string, Guid> PlayerByConnection { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ConcurrentDictionary<Guid, int> ScoreByPlayer { get; } = new();

        public ConcurrentDictionary<Guid, bool> AnsweredThisQuestion { get; } = new();

        public int CurrentQuestionIndex { get; private set; } = -1;

        public DateTimeOffset? CurrentQuestionStartUtc { get; private set; }

        public int CurrentQuestionDurationSeconds { get; private set; } = 20;

        public DateTimeOffset? NextQuestionStartUtc { get; private set; }

        public SessionRuntime(QuizSession session) { Session = session; }

        public QuizQuestion? CurrentQuestion => (Session?.Quiz?.Questions != null && CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Session.Quiz.Questions.Count)
            ? Session.Quiz.Questions.OrderBy(question => question.Id).ElementAt(CurrentQuestionIndex)
            : null;

        public QuizQuestion? NextQuestion => (Session?.Quiz?.Questions != null && CurrentQuestionIndex + 1 < Session.Quiz.Questions.Count)
            ? Session.Quiz.Questions.OrderBy(question => question.Id).ElementAt(CurrentQuestionIndex + 1)
            : null;


        public void ClaimHost(string connectionId) => HostConnectionId = connectionId;

        public void RegisterPlayer(string connectionId, Guid playerId) { PlayerByConnection[connectionId] = playerId; ScoreByPlayer.TryAdd(playerId, 0); }

        public void SetUpcoming(DateTimeOffset startsUtc) { NextQuestionStartUtc = startsUtc; }

        public void ClearUpcoming() { NextQuestionStartUtc = null; }

        public void BeginQuestionNow(int durationSeconds) { ClearUpcoming(); CurrentQuestionIndex++; CurrentQuestionStartUtc = DateTimeOffset.UtcNow; CurrentQuestionDurationSeconds = durationSeconds; AnsweredThisQuestion.Clear(); }
        // Explicitly begin a question by index and set the timer.
        public void BeginQuestionAt(int questionIndex, int durationSeconds)
        {
            ClearUpcoming();
            CurrentQuestionIndex = questionIndex;
            CurrentQuestionStartUtc = DateTimeOffset.UtcNow;
            CurrentQuestionDurationSeconds = durationSeconds;
            AnsweredThisQuestion.Clear();
        }


        public void EndQuestion() { CurrentQuestionStartUtc = null; CurrentQuestionDurationSeconds = 0; AnsweredThisQuestion.Clear(); }

        public void MarkAnswered(Guid playerId) { AnsweredThisQuestion[playerId] = true; }

        public bool HasAnswered(Guid playerId) => AnsweredThisQuestion.TryGetValue(playerId, out var value) && value;
    }
}