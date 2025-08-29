using Microsoft.AspNetCore.SignalR;
using Quizzy.Core;
using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using Quizzy.Web.Models;
using Quizzy.Web.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Quizzy.Web.Hubs
{
    /// <summary>
    /// SignalR hub that coordinates the live quiz flow using Core entities + repositories,
    /// and a lightweight in-memory SessionCoordinator for real-time sync.
    /// </summary>
    public class GameHub : Hub
    {
        private readonly SessionCoordinator _sessions;
        private readonly IUnitOfWork _unitOfWork;

        // Default per-question duration in seconds
        private const int DefaultQuestionDuration = 20;

        public GameHub(SessionCoordinator sessions, IUnitOfWork unitOfWork)
        {
            _sessions = sessions;
            _unitOfWork = unitOfWork;
        }

        // ---------------- UTIL & DTOs ----------------

        private static RoomStateDto BuildStateDto(string gamePin, Services.SessionRuntime runtime, IEnumerable<QuizPlayer> dbPlayers)
        {
            var current = runtime.CurrentQuestion;
            var next = runtime.NextQuestion;

            var playersDto = dbPlayers
                .OrderBy(p => p.Name)
                .Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    score = runtime.ScoreByPlayer.TryGetValue(p.Id, out var s) ? s : 0,
                    hasAnswered = runtime.HasAnswered(p.Id)
                })
                .Cast<object>()
                .ToArray();

            return new RoomStateDto
            {
                RoomId = gamePin.ToUpperInvariant(),
                Host = string.IsNullOrEmpty(runtime.HostConnectionId) ? null : "Host",
                Players = playersDto,
                Question = current == null ? null : new RoomStateDto.QuestionBlock(
                    current.Text,
                    current.Answers.OrderBy(a => a.Id).Select(a => a.Text).ToArray(),
                    runtime.CurrentQuestionDurationSeconds,
                    runtime.CurrentQuestionStartUtc ?? DateTimeOffset.UtcNow
                ),
                Upcoming = next == null || runtime.NextQuestionStartUtc == null ? null : new RoomStateDto.UpcomingBlock(
                    next.Text,
                    next.Answers.OrderBy(a => a.Id).Select(a => a.Text).ToArray(),
                    runtime.NextQuestionStartUtc.Value
                )
            };
        }

        private async Task BroadcastRoomState(string gamePin, Services.SessionRuntime runtime)
        {
            // Pull fresh players from DB to avoid stale names
            var players = await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSession.GamePin == gamePin);
            await Clients.Group(gamePin).SendAsync("RoomStateUpdated", BuildStateDto(gamePin, runtime, players));
        }

        // ---------------- HOST FLOW ----------------
        public async Task<string> CreateAndClaimSession()
        {
            string pin;
            int attempts = 0;
            do
            {
                pin = GeneratePin(6);
                attempts++;
            } while ((await _unitOfWork.QuizSessions.FindAsync(s => s.GamePin == pin)).Any() && attempts < 20);

            var runtime = _sessions.GetOrCreate(pin, () => EnsureSessionForPin(pin));
            runtime.ClaimHost(Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, pin);
            await BroadcastRoomState(pin, runtime);
            return pin;
        }

        private static string GeneratePin(int len)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var r = Random.Shared;
            return new string(Enumerable.Range(0, len).Select(_ => chars[r.Next(chars.Length)]).ToArray());
        }

        public async Task ClaimHost(string gamePin)
        {
            if (string.IsNullOrWhiteSpace(gamePin)) throw new ArgumentException("gamePin required");

            // Load or create a QuizSession for this pin; seed quiz if needed
            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            runtime.ClaimHost(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await BroadcastRoomState(gamePin, runtime);
        }

        public async Task ScheduleNextQuestion(string gamePin, int inSeconds)
        {
            if (!_sessions.TryGet(gamePin, out var runtime)) return;

            var next = runtime.NextQuestion ?? runtime.CurrentQuestion; // if no "next", show current as preview
            if (next == null) return;

            var startUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(1, inSeconds));
            runtime.SetUpcoming(startUtc);

            // Fire-and-forget a task to start question at schedule time
            _ = Task.Run(async () => {
                var delayMs = Math.Max(0, (int)(startUtc - DateTimeOffset.UtcNow).TotalMilliseconds);
                await Task.Delay(delayMs);
                await StartNextQuestionNow(gamePin);
            });

            await BroadcastRoomState(gamePin, runtime);
        }

        public async Task StartNextQuestionNow(string gamePin)
        {
            if (!_sessions.TryGet(gamePin, out var runtime)) return;

            var totalQuestions = runtime.Session?.Quiz?.Questions?.Count ?? 0;
            if (runtime.CurrentQuestionIndex + 1 >= totalQuestions) return; // no more questions

            runtime.BeginQuestionNow(DefaultQuestionDuration);
            await BroadcastRoomState(gamePin, runtime);

            // Automatically end question when timer elapses
            _ = Task.Run(async () => {
                var endAt = runtime.CurrentQuestionStartUtc!.Value.AddSeconds(runtime.CurrentQuestionDurationSeconds);
                var delayMs = Math.Max(0, (int)(endAt - DateTimeOffset.UtcNow).TotalMilliseconds);
                await Task.Delay(delayMs);
                await EndCurrentQuestion(gamePin);
            });
        }

        // ---------------- PLAYER FLOW ----------------

        public async Task JoinAsPlayer(string gamePin, string name, UserAccount userAccount)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new ArgumentException("The Game Pin is required");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name required");
            }
                
            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));

            // Ensure a QuizPlayer exists in DB for this session
            var session = runtime.Session;
            var player = (await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSessionId == session.Id && p.Name == name)).FirstOrDefault();
            if (player == null)
            {
                player = (await _unitOfWork.QuizPlayers.FindAsync(p => p.Name == name)).FirstOrDefault();
                if (player == null)
                {
                    throw new ArgumentException("The name entered does not match with any user's names.");
                }
                
            }

            runtime.RegisterPlayer(Context.ConnectionId, player.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await BroadcastRoomState(gamePin, runtime);
        }

        /// <summary>
        /// Called by the client when clicking an answer option.
        /// </summary>
        public async Task SubmitAnswer(string gamePin, int optionIndex)
        {
            if (!_sessions.TryGet(gamePin, out var runtime)) return;
            var question = runtime.CurrentQuestion;
            if (question == null || runtime.CurrentQuestionStartUtc == null) return;

            // Find player by connection
            if (!runtime.PlayerByConnection.TryGetValue(Context.ConnectionId, out var playerId)) return;
            var player = await _unitOfWork.QuizPlayers.GetByIdAsync(playerId);
            if (player == null) return;

            // Prevent duplicate answers for the current question
            if (runtime.HasAnswered(player.Id)) return;

            // Map option index -> QuizAnswer
            var answersOrdered = question.Answers.OrderBy(a => a.Id).ToArray();
            if (optionIndex < 0 || optionIndex >= answersOrdered.Length) return;
            var chosen = answersOrdered[optionIndex];

            // Compute points: correct => base + speed bonus, else 0
            int points = 0;
            if (chosen.IsCorrect)
            {
                var elapsed = (int)(DateTimeOffset.UtcNow - runtime.CurrentQuestionStartUtc.Value).TotalMilliseconds;
                var totalMs = runtime.CurrentQuestionDurationSeconds * 1000;
                var remainingRatio = Math.Max(0.0, (totalMs - elapsed) / (double)totalMs);
                points = 1000 + (int)(500 * remainingRatio); // 1000-1500 depending on speed
            }

            // Persist PlayerAnswer
            var pa = new PlayerAnswer
            {
                Id = Guid.NewGuid(),
                PlayerId = player.Id,
                QuestionId = question.Id,
                AnswerId = chosen.Id,
                DateTime = DateTime.UtcNow,
                PointsValue = points
            };
            await _unitOfWork.PlayerAnswers.AddAsync(pa);
            await _unitOfWork.SaveChangesAsync();

            // Update runtime state
            runtime.MarkAnswered(player.Id);
            runtime.ScoreByPlayer.AddOrUpdate(player.Id, points, (_, prev) => prev + points);

            // Broadcast fresh state so the player sees "answered" + live leaderboard movement
            await BroadcastRoomState(gamePin, runtime);
        }

        /// <summary>
        /// Ends the current question, broadcasts per-option counts and leaderboard.
        /// </summary>
        public async Task EndCurrentQuestion(string gamePin)
        {
            if (!_sessions.TryGet(gamePin, out var runtime)) return;
            var question = runtime.CurrentQuestion;
            if (question == null) return;

            // Aggregate option counts from persisted PlayerAnswers
            var answersOrdered = question.Answers.OrderBy(a => a.Id).ToArray();
            var optionCounts = new int[answersOrdered.Length];

            var sessionPlayerIds = (await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSession.GamePin == gamePin)).Select(p => p.Id).ToHashSet();
            var playerAnswers = await _unitOfWork.PlayerAnswers.FindAsync(a => sessionPlayerIds.Contains(a.PlayerId) && a.QuestionId == question.Id);

            foreach (var playerAnswer in playerAnswers)
            {
                var idx = Array.FindIndex(answersOrdered, answer => answer.Id == playerAnswer.AnswerId);
                if (idx >= 0) optionCounts[idx]++;
            }

            var players = await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSession.GamePin == gamePin);
            var leaderboard = players
                .Select(player => (player.Name, Score: runtime.ScoreByPlayer.TryGetValue(player.Id, out var score) ? score : 0))
                .OrderByDescending(x => x.Score).ThenBy(x => x.Name)
                .ToArray();

            runtime.EndQuestion();

            await Clients.Group(gamePin).SendAsync("QuestionEnded",
                new QuestionSummaryDto(
                    correctIndex: Array.FindIndex(answersOrdered, answer => answer.IsCorrect),
                    optionCounts: optionCounts,
                    leaderboard: leaderboard
                )
            );

            await BroadcastRoomState(gamePin, runtime);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _sessions.RemoveConnection(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        // ---------------- helpers ----------------

        private QuizSession EnsureSessionForPin(string gamePin)
        {
            // Try existing
            var session = _unitOfWork.QuizSessions.FindAsync(s => s.GamePin == gamePin).GetAwaiter().GetResult().FirstOrDefault();
            if (session != null)
            {
                return session;
            }

                // Ensure a host user exists (required FK QuizHostId)
            var host = _unitOfWork.UserAccounts.FindAsync(u => u.Username == "host").GetAwaiter().GetResult().FirstOrDefault();
            if (host == null)
            {
                //host = new UserAccount { Id = Guid.NewGuid(), Username = "host", Password = "n/a", Email = "host@example.com" };
                //_unitOfWork.UserAccounts.AddAsync(host).GetAwaiter().GetResult();
                //_unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
                throw new InvalidOperationException("A host was not found with this game pin");
            }

            // Ensure there is at least one quiz
            //var quiz = _unitOfWork.Quizzes.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
            //var quiz = GETSELECTEDQUIZ
            //if (quiz == null) quiz = SeedDefaultQuiz();

            //// Create session with required foreign keys set
            //session = new QuizSession
            //{
            //    Id = Guid.NewGuid(),
            //    GamePin = gamePin.ToUpperInvariant(),
            //    QuizId = quiz.Id,
            //    QuizHostId = host.Id
            //};
            //_unitOfWork.QuizSessions.AddAsync(session).GetAwaiter().GetResult();
            //_unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            return session;
        }


        private Quiz SeedDefaultQuiz()
        {
            var author = _unitOfWork.UserAccounts.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
            if (author == null)
            {
                author = new UserAccount { Id = Guid.NewGuid(), Username = "host", Password = "n/a", Email = "host@example.com" };
                _unitOfWork.UserAccounts.AddAsync(author).GetAwaiter().GetResult();
            }

            var quiz = new Quiz
            {
                Id = Guid.NewGuid(),
                Title = "Sample Quiz",
                QuizAuthorId = author.Id,
                QuizAuthor = author
            };

            var q1 = new QuizQuestion { Id = Guid.NewGuid(), Text = "2 + 2 = ?", Quiz = quiz, QuizId = quiz.Id };
            var a1 = new QuizAnswer { Id = Guid.NewGuid(), Text = "3", Question = q1, QuestionId = q1.Id, IsCorrect = false };
            var a2 = new QuizAnswer { Id = Guid.NewGuid(), Text = "4", Question = q1, QuestionId = q1.Id, IsCorrect = true };
            var a3 = new QuizAnswer { Id = Guid.NewGuid(), Text = "5", Question = q1, QuestionId = q1.Id, IsCorrect = false };
            q1.Answers.Add(a1); q1.Answers.Add(a2); q1.Answers.Add(a3);

            var q2 = new QuizQuestion { Id = Guid.NewGuid(), Text = "Capital of Australia?", Quiz = quiz, QuizId = quiz.Id };
            var b1 = new QuizAnswer { Id = Guid.NewGuid(), Text = "Sydney", Question = q2, QuestionId = q2.Id, IsCorrect = false };
            var b2 = new QuizAnswer { Id = Guid.NewGuid(), Text = "Canberra", Question = q2, QuestionId = q2.Id, IsCorrect = true };
            var b3 = new QuizAnswer { Id = Guid.NewGuid(), Text = "Melbourne", Question = q2, QuestionId = q2.Id, IsCorrect = false };
            q2.Answers.Add(b1); q2.Answers.Add(b2); q2.Answers.Add(b3);

            var q3 = new QuizQuestion { Id = Guid.NewGuid(), Text = "Select the prime number", Quiz = quiz, QuizId = quiz.Id };
            var c1 = new QuizAnswer { Id = Guid.NewGuid(), Text = "9", Question = q3, QuestionId = q3.Id, IsCorrect = false };
            var c2 = new QuizAnswer { Id = Guid.NewGuid(), Text = "11", Question = q3, QuestionId = q3.Id, IsCorrect = true };
            var c3 = new QuizAnswer { Id = Guid.NewGuid(), Text = "15", Question = q3, QuestionId = q3.Id, IsCorrect = false };
            q3.Answers.Add(c1); q3.Answers.Add(c2); q3.Answers.Add(c3);

            quiz.Questions.Add(q1); quiz.Questions.Add(q2); quiz.Questions.Add(q3);

            _unitOfWork.Quizzes.AddAsync(quiz).GetAwaiter().GetResult();
            _unitOfWork.QuizQuestions.AddAsync(q1).GetAwaiter().GetResult();
            _unitOfWork.QuizQuestions.AddAsync(q2).GetAwaiter().GetResult();
            _unitOfWork.QuizQuestions.AddAsync(q3).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(a1).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(a2).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(a3).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(b1).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(b2).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(b3).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(c1).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(c2).GetAwaiter().GetResult();
            _unitOfWork.QuizAnswers.AddAsync(c3).GetAwaiter().GetResult();
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();

            return quiz;
        }
    }
}