using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Quizzy.Core.Entities;

namespace Quizzy.Web.Services
{
    /// <summary>
    /// In-memory runtime state & orchestration for live games.
    /// This keeps ONLY ephemeral data (connections, timers, scores, etc).
    /// Persistent data (players, player answers, quizzes) stays in Quizzy.Core via repositories.
    /// </summary>
    public class SessionCoordinator
    {
        private readonly ConcurrentDictionary<string, SessionRuntime> _sessions = new(StringComparer.OrdinalIgnoreCase);

        public SessionRuntime GetOrCreate(string gamePin, Func<QuizSession> sessionFactory)
        {
            return _sessions.GetOrAdd(gamePin, _ => new SessionRuntime(sessionFactory()));
        }

        public bool TryGet(string gamePin, out SessionRuntime runtime) => _sessions.TryGetValue(gamePin, out runtime);

        public void RemoveConnection(string connectionId)
        {
            foreach (var kvp in _sessions)
            {
                var rt = kvp.Value;
                rt.PlayerByConnection.TryRemove(connectionId, out _);
                if (rt.HostConnectionId == connectionId) rt.HostConnectionId = string.Empty;
            }
        }
    }

    /// <summary>
    /// Live/ephemeral state for a running session.
    /// </summary>
    public class SessionRuntime
    {
        public QuizSession Session { get; }

        public string HostConnectionId { get; set; } = string.Empty;
        public ConcurrentDictionary<string, Guid> PlayerByConnection { get; } = new(StringComparer.OrdinalIgnoreCase);
        public ConcurrentDictionary<Guid, int> ScoreByPlayer { get; } = new(); // PlayerId -> Score
        public ConcurrentDictionary<Guid, bool> AnsweredThisQuestion { get; } = new(); // PlayerId -> answered

        public int CurrentQuestionIndex { get; private set; } = -1;
        public DateTimeOffset? CurrentQuestionStartUtc { get; private set; }
        public int CurrentQuestionDurationSeconds { get; private set; } = 20;

        public DateTimeOffset? NextQuestionStartUtc { get; private set; }

        private CancellationTokenSource? _questionCts;
        private CancellationTokenSource? _scheduleCts;

        public SessionRuntime(QuizSession session)
        {
            Session = session;
        }

        public QuizQuestion? CurrentQuestion =>
            (Session?.Quiz?.Questions != null && CurrentQuestionIndex >= 0 && CurrentQuestionIndex < Session.Quiz.Questions.Count)
                ? Session.Quiz.Questions.OrderBy(q => q.Id).ElementAt(CurrentQuestionIndex)
                : null;

        public QuizQuestion? NextQuestion =>
            (Session?.Quiz?.Questions != null && CurrentQuestionIndex + 1 < Session.Quiz.Questions.Count)
                ? Session.Quiz.Questions.OrderBy(q => q.Id).ElementAt(CurrentQuestionIndex + 1)
                : null;

        public void ClaimHost(string connectionId)
        {
            HostConnectionId = connectionId;
        }

        public void RegisterPlayer(string connectionId, Guid playerId)
        {
            PlayerByConnection[connectionId] = playerId;
            ScoreByPlayer.TryAdd(playerId, 0);
        }

        public void SetUpcoming(DateTimeOffset startsUtc)
        {
            _scheduleCts?.Cancel();
            _scheduleCts = new CancellationTokenSource();
            NextQuestionStartUtc = startsUtc;
        }

        public void ClearUpcoming()
        {
            _scheduleCts?.Cancel();
            NextQuestionStartUtc = null;
        }

        public void BeginQuestionNow(int durationSeconds = 20)
        {
            ClearUpcoming();
            _questionCts?.Cancel();
            _questionCts = new CancellationTokenSource();

            CurrentQuestionIndex++;
            CurrentQuestionStartUtc = DateTimeOffset.UtcNow;
            CurrentQuestionDurationSeconds = durationSeconds;
            AnsweredThisQuestion.Clear();
        }

        public void EndQuestion()
        {
            _questionCts?.Cancel();
            CurrentQuestionStartUtc = null;
            CurrentQuestionDurationSeconds = 0;
            AnsweredThisQuestion.Clear();
        }

        public void MarkAnswered(Guid playerId)
        {
            AnsweredThisQuestion[playerId] = true;
        }

        public bool HasAnswered(Guid playerId) => AnsweredThisQuestion.TryGetValue(playerId, out var v) && v;
    }
}