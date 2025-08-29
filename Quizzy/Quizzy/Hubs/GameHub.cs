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

        private static SessionStateDto BuildStateDto(string gamePin, Services.SessionRuntime runtime, IEnumerable<QuizPlayer> dbPlayers)
        {
            var current = runtime.CurrentQuestion;
            var next = runtime.NextQuestion;

            var playersDto = dbPlayers
                .OrderBy(player => player.Name)
                .Select(player => new
                {
                    id = player.Id,
                    name = player.Name,
                    score = runtime.ScoreByPlayer.TryGetValue(player.Id, out var score) ? score : 0,
                    hasAnswered = runtime.HasAnswered(player.Id)
                })
                .Cast<object>()
                .ToArray();

            return new SessionStateDto
            {
                RoomId = gamePin.ToUpperInvariant(),
                Host = string.IsNullOrEmpty(runtime.HostConnectionId) ? null : "Host",
                Players = playersDto,
                Question = current == null
                    ? null
                    : new SessionStateDto.QuestionBlock(
                        current.Text,
                        current.Answers.OrderBy(answer => answer.Id).Select(answer => answer.Text).ToArray(),
                        runtime.CurrentQuestionDurationSeconds,
                        runtime.CurrentQuestionStartUtc ?? DateTimeOffset.UtcNow
                    ),
                Upcoming = next == null || runtime.NextQuestionStartUtc == null
                ? null
                : new SessionStateDto.UpcomingBlock(
                    next.Text,
                    next.Answers.OrderBy(answer => answer.Id).Select(answer => answer.Text).ToArray(),
                    runtime.NextQuestionStartUtc.Value
                )
            };
        }

        private async Task BroadcastSessionState(string gamePin, Services.SessionRuntime runtime)
        {
            // Pull fresh players from DB to avoid stale names
            var players = await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSession.GamePin == gamePin);
            await Clients.Group(gamePin).SendAsync("SessionStateUpdated", BuildStateDto(gamePin, runtime, players));
        }

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
            await BroadcastSessionState(pin, runtime);
            return pin;
        }

        public async Task ClaimHost(string gamePin)
        {
            if (string.IsNullOrWhiteSpace(gamePin)) throw new ArgumentException("gamePin required");

            // Load or create a QuizSession for this pin; seed quiz if needed
            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            runtime.ClaimHost(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await BroadcastSessionState(gamePin, runtime);
        }

        private static string GeneratePin(int len)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var r = Random.Shared;
            return new string(Enumerable.Range(0, len).Select(_ => chars[r.Next(chars.Length)]).ToArray());
        }

        private async Task<UserAccount> ResolveHostAccountAsync(string? hostUserIdOrName)
        {
            if (!string.IsNullOrWhiteSpace(hostUserIdOrName))
            {
                if (Guid.TryParse(hostUserIdOrName, out var gid))
                {
                    var byGuid = await _unitOfWork.UserAccounts.GetByIdAsync(gid);
                    if (byGuid != null) return byGuid;
                }
                var byName = (await _unitOfWork.UserAccounts.FindAsync(u => u.Username == hostUserIdOrName)).FirstOrDefault();
                if (byName != null) return byName;
            }

            var uname = Context?.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(uname))
            {
                var byPrincipal = (await _unitOfWork.UserAccounts.FindAsync(u => u.Username == uname)).FirstOrDefault();
                if (byPrincipal != null) return byPrincipal;
            }

            throw new HubException("Host account not found. Please log in.");
        }

        private QuizSession EnsureSessionForPinWithHostAndQuiz(string gamePin, UserAccount host, Guid quizId)
        {
            var session = _unitOfWork.QuizSessions.FindAsync(s => s.GamePin == gamePin).GetAwaiter().GetResult().FirstOrDefault();
            if (session != null) return session;

            var quiz = _unitOfWork.Quizzes.GetByIdAsync(quizId).GetAwaiter().GetResult();
            if (quiz == null) throw new InvalidOperationException("Quiz not found");

            session = new QuizSession
            {
                Id = Guid.NewGuid(),
                GamePin = gamePin.ToUpperInvariant(),
                QuizId = quiz.Id,
                QuizHostId = host.Id
            };
            _unitOfWork.QuizSessions.AddAsync(session).GetAwaiter().GetResult();
            _unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            // attach navigation for runtime
            session.Quiz = quiz;
            return session;
        }

        public async Task<string> CreateAndClaimSessionForQuiz(string hostUserId, Guid quizId)
        {
            UserAccount hostAccount = await ResolveHostAccountAsync(hostUserId);

            string pin = GeneratePin(6);
            int attempts = 0;

            while ((await _unitOfWork.QuizSessions.FindAsync(sessionEntity => sessionEntity.GamePin == pin)).Any() && attempts < 50)
            {
                pin = GeneratePin(6);
                attempts++;
            }

            var runtime = _sessions.GetOrCreate(pin, () => EnsureSessionForPinWithHostAndQuiz(pin, hostAccount, quizId));

            runtime.ClaimHost(Context.ConnectionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, pin);

            await BroadcastSessionState(pin, runtime);

            return pin;
        }

        public async Task StartNextQuestionNow(string gamePin)
        {
            await ScheduleNextQuestion(gamePin, 0);
        }

        private QuizSession EnsureSessionForPin(string gamePin)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new HubException("A room code is required.");
            }

            string pinUpper = gamePin.Trim().ToUpperInvariant();

            QuizSession? sessionEntity = _unitOfWork.QuizSessions
                .FindAsync(sessionEntity => sessionEntity.GamePin == pinUpper)
                .GetAwaiter()
                .GetResult()
                .FirstOrDefault();

            if (sessionEntity == null)
            {
                throw new HubException($"No live session found for code '{pinUpper}'. Ask the host to start a new session.");
            }

            if (sessionEntity.Quiz == null)
            {
                sessionEntity.Quiz = _unitOfWork.Quizzes
                    .GetByIdAsync(sessionEntity.QuizId)
                    .GetAwaiter()
                    .GetResult();
            }

            return sessionEntity;
        }

        public async Task ScheduleNextQuestion(string gamePin, int inSeconds)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new HubException("A room code is required.");
            }

            if (inSeconds < 0)
            {
                inSeconds = 0;
            }

            // Get or materialize the runtime from the existing DB session.
            // Players should never create sessions here; this uses the fetch-or-fail helper.
            SessionRuntime runtime;
            if (!_sessions.TryGet(gamePin, out runtime))
            {
                QuizSession sessionEntity = EnsureSessionForPin(gamePin);
                runtime = _sessions.GetOrCreate(gamePin, () => sessionEntity);
            }

            // Decide the start UTC and inform the runtime so clients see "upcoming".
            DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddSeconds(inSeconds);
            runtime.SetUpcoming(startUtc);

            // Tell everyone (host + players) about the upcoming start immediately.
            await BroadcastSessionState(gamePin, runtime);

            // Fire-and-forget a timer that flips to the live question exactly at startUtc.
            _ = Task.Run(async () =>
            {
                int delayMilliseconds = Math.Max(0, (int)(startUtc - DateTimeOffset.UtcNow).TotalMilliseconds);

                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds);
                }

                try
                {
                    await StartNextQuestionNow(gamePin);
                }
                catch (Exception exception)
                {
                    // Optional: log exception (e.g., _logger.LogError(exception, "Failed to start next question for {Pin}", gamePin))
                }
            });
        }

        public async Task JoinAsPlayer(string gamePin, string name, UserAccount userAccount)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new ArgumentException("The Game Pin is required");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name is required");
            }

            if (userAccount == null || userAccount.Id == Guid.Empty)
            {
                throw new HubException("Please log in before joining.");
            }

            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            var session = runtime.Session;

            var account = await _unitOfWork.UserAccounts.GetByIdAsync(userAccount.Id);
            if (account == null)
            {
                throw new HubException("User account not found.");
            }

            var existing = (await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSessionId == session.Id && p.UserAccountId == account.Id)).FirstOrDefault();

            QuizPlayer player;
            if (existing == null)
            {
                var baseName = name.Trim();
                var candidate = baseName;
                var i = 2;

                while ((await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSessionId == session.Id && p.Name == candidate)).Any())
                {
                    candidate = $"{baseName} ({i++})";
                }

                player = new QuizPlayer
                {
                    Id = Guid.NewGuid(),
                    Name = candidate,
                    UserAccountId = account.Id,
                    QuizSessionId = session.Id
                };

                await _unitOfWork.QuizPlayers.AddAsync(player);
                await _unitOfWork.SaveChangesAsync();
            }
            else
            {
                player = existing;

                if (!string.Equals(player.Name, name, StringComparison.Ordinal))
                {
                    player.Name = name;
                    _unitOfWork.QuizPlayers.Update(player);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            runtime.RegisterPlayer(Context.ConnectionId, player.Id);

            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);

            await BroadcastSessionState(gamePin, runtime);
        }
    }
}
