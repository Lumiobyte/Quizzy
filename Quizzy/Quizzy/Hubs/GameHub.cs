using Microsoft.AspNetCore.SignalR;
using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using Quizzy.Web.Services;
using Quizzy.Core.DTOs;

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
        private readonly IServiceScopeFactory _scopeFactory;

        // Default per-question duration in seconds
        private const int DefaultQuestionDuration = 20;

        public GameHub(SessionCoordinator sessions, IUnitOfWork unitOfWork, IServiceScopeFactory scopeFactory)
        {
            _sessions = sessions;
            _unitOfWork = unitOfWork;
            _scopeFactory = scopeFactory;
        }

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
                SessionId = gamePin.ToUpperInvariant(),
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

        public async Task ScheduleNextQuestion(string gamePin, int inSeconds, int questionIndex)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new HubException("A room code is required.");
            }

            if (inSeconds < 0)
            {
                inSeconds = 0;
            }

            // Get or materialize the runtime for this pin.
            SessionRuntime runtime;
            if (!_sessions.TryGet(gamePin, out runtime))
            {
                QuizSession sessionEntity = EnsureSessionForPin(gamePin);
                runtime = _sessions.GetOrCreate(gamePin, () => sessionEntity);
            }

            // Decide the start UTC and inform clients now (so they show the same countdown).
            DateTimeOffset startUtc = DateTimeOffset.UtcNow.AddSeconds(inSeconds);
            runtime.SetUpcoming(startUtc);

            await BroadcastSessionState(gamePin, runtime);

            // Server-driven flip at startUtc using a FRESH scope and IHubContext.
            _ = Task.Run(async () =>
            {
                int delayMilliseconds = Math.Max(0, (int)(startUtc - DateTimeOffset.UtcNow).TotalMilliseconds);

                if (delayMilliseconds > 0)
                {
                    await Task.Delay(delayMilliseconds);
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();

                    // Read data using the fresh DbContext
                    List<QuizQuestion> quizQuestions = await GetQuizQuestionsAsync(unitOfWork, gamePin);
                    QuizQuestion question = GetQuizQuestionAtIndex(questionIndex, quizQuestions);

                    QuestionStartedDto dto = BuildQuestionStartedDto(question);

                    // Broadcast using the fresh hub context (NOT this hub instance)
                    await hubContext.Clients.Group(gamePin).SendAsync("StartNextQuestion", dto);
                }
                catch (Exception exception)
                {
                    // Log properly in your app; this avoids taking down the process
                    Console.WriteLine($"{exception}\nFailed to start next question for {gamePin}");
                }
            });
        }

        private static QuestionStartedDto BuildQuestionStartedDto(QuizQuestion question)
        {
            if (question == null)
            {
                throw new ArgumentNullException(nameof(question));
            }

            var answers = question.Answers ?? Array.Empty<QuizAnswer>();

            var answerStrings = answers
                .OrderBy(a => a.Text)
                .Select(a => a?.Text ?? string.Empty)
                .Select(text => text.Trim())
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();


            return new QuestionStartedDto
            {
                Question = question.Text ?? string.Empty,
                Options = answerStrings,
                QuestionType = question.QuestionType,
                DurationSeconds = 20,
                StartTimeOffset = DateTimeOffset.UtcNow
            };
        }


        private static async Task<List<QuizQuestion>> GetQuizQuestionsAsync(IUnitOfWork unitOfWork, string gamePin)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new ArgumentException("Pin is required.", nameof(gamePin));
            }

            string pinUpper = gamePin.Trim().ToUpperInvariant();

            var session = (await unitOfWork.QuizSessions.FindAsync(s => s.GamePin == pinUpper)).FirstOrDefault();
            if (session == null)
            {
                throw new HubException($"No live session found for code '{pinUpper}'.");
            }

            // Load quiz and its questions
            if (session.Quiz == null)
            {
                session.Quiz = await unitOfWork.Quizzes.GetByIdWithDetailsAsync(session.QuizId);
            }

            var questions = session.Quiz?.Questions?.ToList() ?? new List<QuizQuestion>();

            // Ensure each question has answers materialized (order however you want)
            foreach (var q in questions)
            {
                if (q.Answers == null || !q.Answers.Any())
                {
                    q.Answers = (await unitOfWork.QuizAnswers.FindAsync(a => a.QuestionId == q.Id)).ToList();
                }
            }

            return questions;
        }

        private static QuizQuestion GetQuizQuestionAtIndex(int index, IList<QuizQuestion> questions)
        {
            if (questions == null)
            {
                throw new ArgumentNullException(nameof(questions));
            }

            if (index < 0 || index >= questions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "The index is out of range of the quiz questions.");
            }

            return questions[index];
        }

        public async Task JoinAsPlayer(string gamePin, string name, Guid playerGuid)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new ArgumentException("The Game Pin is required");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name is required");
            }

            if (playerGuid == Guid.Empty)
            {
                throw new HubException("Please log in before joining.");
            }

            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            var session = runtime.Session;

            var account = await _unitOfWork.UserAccounts.GetByIdAsync(playerGuid);
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
