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
        private const int DefaultQuestionDuration = 10; // CHANGE DURATION OF QUIZ HERE

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
                Quiz = quiz,
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
                throw new HubException("A room code is required.");
            if (inSeconds < 0) inSeconds = 0;

            // Get or materialize runtime
            if (!_sessions.TryGet(gamePin, out var runtime))
            {
                var sessionEntity = EnsureSessionForPin(gamePin);
                runtime = _sessions.GetOrCreate(gamePin, () => sessionEntity);
            }

            // Tell clients when the next question will start (for countdown UI)
            var startUtc = DateTimeOffset.UtcNow.AddSeconds(inSeconds);
            runtime.SetUpcoming(startUtc);
            await BroadcastSessionState(gamePin, runtime);

            // Flip live at startUtc
            _ = Task.Run(async () =>
            {
                var delayMs = Math.Max(0, (int)(startUtc - DateTimeOffset.UtcNow).TotalMilliseconds);
                if (delayMs > 0) await Task.Delay(delayMs);

                try
                {
                    // Fresh scope for "start" work
                    using var startScope = _scopeFactory.CreateScope();
                    var startUow = startScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var hubContext = startScope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();

                    // Load quiz + questions
                    var quizQuestions = await GetQuizQuestionsAsync(startUow, gamePin);

                    // Decide the server-truth next index (don’t trust caller blindly)
                    var nextIndex = 0;
                    if (quizQuestions.Count > 0)
                        nextIndex = Math.Min(Math.Max(0, runtime.CurrentQuestionIndex + 1), quizQuestions.Count - 1);

                    var question = GetQuizQuestionAtIndex(nextIndex, quizQuestions);

                    // Build DTO and begin question now (sets index + timer + clears answered flags)
                    var dto = BuildQuestionStartedDto(question);
                    runtime.BeginQuestionAt(nextIndex, dto.DurationSeconds);
                    var scheduledQuestionIndex = nextIndex;
                    var autoEndToken = runtime.AutoEndTokenSource.Token;

                    // Broadcast the question
                    await hubContext.Clients.Group(gamePin).SendAsync("StartNextQuestion", dto);

                    // Capture minimal IDs ONLY (don’t capture DbContext or tracked entities across the delay)
                    var questionId = question.Id;
                    var quizSessionId = runtime.Session.Id;

                    // Auto-end after duration with a *new scope* so DbContext is valid
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(dto.DurationSeconds), autoEndToken);
                            if (scheduledQuestionIndex != runtime.CurrentQuestionIndex) return;

                            if (runtime.CurrentQuestionStartUtc == null)
                                return;

                            using var endScope = _scopeFactory.CreateScope();
                            var endUow = endScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            var endHub = endScope.ServiceProvider.GetRequiredService<IHubContext<GameHub>>();

                            // Reload everything needed INSIDE this fresh scope
                            var answersOrdered = (await endUow.QuizAnswers.FindAsync(a => a.QuestionId == questionId))
                                .OrderBy(a => a.Text)
                                .ToList();

                            var correctIndex = answersOrdered.FindIndex(a => a.IsCorrect);

                            var players = (await endUow.QuizPlayers.FindAsync(p => p.QuizSessionId == quizSessionId)).ToList();
                            var playerIds = players.Select(p => p.Id).ToHashSet();

                            var allAnswers = (await endUow.PlayerAnswers.FindAsync(a => a.QuestionId == questionId)).ToList();

                            var nameByPlayer = players.ToDictionary(p => p.Id, p => p.Name ?? "");

                            var counts = Enumerable.Repeat(0, answersOrdered.Count).ToArray();
                            foreach (var pa in allAnswers)
                            {
                                if (!playerIds.Contains(pa.PlayerId)) continue;
                                var idx = answersOrdered.FindIndex(a => a.Id == pa.AnswerId);
                                if (idx >= 0) counts[idx]++;
                            }

                            var answersDto = allAnswers
                                .Where(pa => playerIds.Contains(pa.PlayerId))
                                .Select(pa => new PlayerAnswerDto(
                                    nameByPlayer.TryGetValue(pa.PlayerId, out var nm) ? nm : string.Empty,
                                    answersOrdered.FindIndex(a => a.Id == pa.AnswerId),
                                    pa.ResponseTime.TotalSeconds))
                                .ToArray();

                            // Leaderboard from in-memory scores (fast) + names from DB
                            var leaderboard = runtime.ScoreByPlayer
                                .OrderByDescending(kv => kv.Value)
                                .ThenBy(kv => nameByPlayer.TryGetValue(kv.Key, out var nm) ? nm : string.Empty, StringComparer.OrdinalIgnoreCase)
                                .Take(50)
                                .Select(kv => new { name = nameByPlayer.TryGetValue(kv.Key, out var nm) ? nm : "(unknown)", score = kv.Value })
                                .ToArray();

                            // Close the question
                            runtime.EndQuestion();

                            // Push results
                            await endHub.Clients.Group(gamePin).SendAsync("QuestionEnded", new
                            {
                                correctIndex,
                                optionCounts = counts,
                                leaderboard,
                                answers = answersDto
                            });

                            // Recreate a minimal SessionState (don’t call BroadcastSessionState here, it uses the hub’s scoped _unitOfWork)
                            var playersDto = players
                                .OrderBy(p => p.Name)
                                .Select(p => new
                                {
                                    id = p.Id,
                                    name = p.Name,
                                    score = runtime.ScoreByPlayer.TryGetValue(p.Id, out var s) ? s : 0,
                                    hasAnswered = runtime.HasAnswered(p.Id)
                                })
                                .Cast<object>()
                                .ToArray();

                            var sessionState = new SessionStateDto
                            {
                                SessionId = gamePin.ToUpperInvariant(),
                                Host = string.IsNullOrEmpty(runtime.HostConnectionId) ? null : runtime.HostConnectionId,
                                Players = playersDto,
                                Question = null,  // ended
                                Upcoming = null   // none scheduled
                            };

                            await endHub.Clients.Group(gamePin).SendAsync("SessionStateUpdated", sessionState);
                        }
                        catch (TaskCanceledException)
                        {
                            // Timer was canceled or question changed so no action needed
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Auto-end failed: {ex}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex}\nFailed to start next question for {gamePin}");
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
                DurationSeconds = DefaultQuestionDuration,
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

            var questions = session.Quiz?.Questions?
                .OrderBy(q => q.OrderIndex)
                .ToList() ?? new List<QuizQuestion>();

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

        public async Task<int> GetQuestionCount(string gamePin)
        {
            var questions = await GetQuizQuestionsAsync(_unitOfWork, gamePin);
            return questions.Count;
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

        public async Task JoinAsPlayer(string gamePin, string name, string playerId)
        {
            _ = Guid.TryParse(playerId, out var playerGuid);

            if (playerGuid == Guid.Empty)
            {
                await JoinAsPlayerWithoutLogin(gamePin, name);
            }

            if (string.IsNullOrWhiteSpace(gamePin))
            {
                throw new ArgumentException("The Game Pin is required");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Player name is required");
            }

            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            var session = runtime.Session;

            var account = await _unitOfWork.UserAccounts.GetByIdAsync(playerGuid);
            //var players = _unitOfWork.UserAccounts.GetAllAsync().Result.ToList();

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

        //No login
        public async Task JoinAsPlayerWithoutLogin(string gamePin, string name)
        {
            throw new NotImplementedException("");
            if (string.IsNullOrWhiteSpace(gamePin)) throw new HubException("Room code is required.");
            if (string.IsNullOrWhiteSpace(name)) throw new HubException("Player name is required.");

            var runtime = _sessions.GetOrCreate(gamePin, () => EnsureSessionForPin(gamePin));
            var session = runtime.Session;

            // Create a transient pseudo-account (or look up by cookie/IP if you prefer)
            var player = new QuizPlayer
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                QuizSessionId = session.Id,
                UserAccountId = Guid.Parse("B5106106-9984-4110-9904-4D2C973F48F6") // set this to a user who isn't logged in
            };
            await _unitOfWork.QuizPlayers.AddAsync(player);
            await _unitOfWork.SaveChangesAsync();

            runtime.RegisterPlayer(Context.ConnectionId, player.Id);
            await Groups.AddToGroupAsync(Context.ConnectionId, gamePin);
            await BroadcastSessionState(gamePin, runtime);
        }


        public async Task SubmitAnswer(string gamePin, int selectedIndex)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
                throw new HubException("A room code is required.");

            if (!_sessions.TryGet(gamePin, out var runtime))
                throw new HubException("Session not found.");

            if (!runtime.PlayerByConnection.TryGetValue(Context.ConnectionId, out var playerId))
                throw new HubException("Player not registered in this session.");

            if (runtime.HasAnswered(playerId))
                return;

            var quizQuestions = await GetQuizQuestionsAsync(_unitOfWork, gamePin);
            var qIdx = runtime.CurrentQuestionIndex;
            if (qIdx < 0 || qIdx >= quizQuestions.Count) return;

            var question = quizQuestions[qIdx];
            var answersOrdered = (question.Answers ?? Array.Empty<QuizAnswer>())
                .OrderBy(a => a.Text)
                .ToList();

            if (selectedIndex < 0 || selectedIndex >= answersOrdered.Count) return;

            var chosen = answersOrdered[selectedIndex];

            var now = DateTimeOffset.UtcNow;
            if (!runtime.FirstAnswerUtc.HasValue)
            {
                runtime.FirstAnswerUtc = now;
            }

            var pa = new PlayerAnswer
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                QuestionId = question.Id,
                AnswerId = chosen.Id,
                ResponseTime = now - runtime.FirstAnswerUtc.Value,
                PointsValue = 0
            };

            await _unitOfWork.PlayerAnswers.AddAsync(pa);

            runtime.MarkAnswered(playerId);

            await _unitOfWork.SaveChangesAsync();

            var totalPlayers = runtime.PlayerByConnection.Values.Distinct().Count();
            var allAnswered = totalPlayers > 0 && runtime.AnsweredThisQuestion.Count >= totalPlayers;

            if (allAnswered)
            {
                await EndCurrentQuestion(gamePin);
                await BroadcastSessionState(gamePin, runtime);
            }
        }

        public async Task EndCurrentQuestion(string gamePin)
        {
            if (string.IsNullOrWhiteSpace(gamePin))
                throw new HubException("A room code is required.");

            if (!_sessions.TryGet(gamePin, out var runtime))
                throw new HubException("Session not found.");

            var session = runtime.Session ?? EnsureSessionForPin(gamePin);
            var quizQuestions = await GetQuizQuestionsAsync(_unitOfWork, gamePin);

            if (runtime.CurrentQuestionIndex < 0 || runtime.CurrentQuestionIndex >= quizQuestions.Count)
            {
                runtime.EndQuestion();
                await Clients.Group(gamePin).SendAsync("QuestionEnded", new
                {
                    correctIndex = -1,
                    optionCounts = Array.Empty<int>(),
                    leaderboard = Array.Empty<object>()
                });
                await BroadcastSessionState(gamePin, runtime);
                return;
            }

            var question = quizQuestions[runtime.CurrentQuestionIndex];

            var answersOrdered = (question.Answers ?? Array.Empty<QuizAnswer>())
                .OrderBy(a => a.Text)
                .ToList();

            var correctIndex = answersOrdered.FindIndex(a => a.IsCorrect);

            var players = (await _unitOfWork.QuizPlayers.FindAsync(p => p.QuizSessionId == session.Id)).ToList();
            var playerIds = players.Select(p => p.Id).ToHashSet();
            var allAnswers = (await _unitOfWork.PlayerAnswers.FindAsync(a => a.QuestionId == question.Id)).ToList();

            var counts = Enumerable.Repeat(0, answersOrdered.Count).ToArray();
            foreach (var pa in allAnswers)
            {
                if (!playerIds.Contains(pa.PlayerId)) continue;
                var idx = answersOrdered.FindIndex(a => a.Id == pa.AnswerId);
                if (idx >= 0) counts[idx]++;
            }

            var nameByPlayer = players.ToDictionary(p => p.Id, p => p.Name ?? "");

            var answersDto = allAnswers
                .Where(pa => playerIds.Contains(pa.PlayerId))
                .Select(pa => new PlayerAnswerDto(
                    nameByPlayer.TryGetValue(pa.PlayerId, out var nm) ? nm : string.Empty,
                    answersOrdered.FindIndex(a => a.Id == pa.AnswerId),
                    pa.ResponseTime.TotalSeconds))
                .ToArray();

            var leaderboard = runtime.ScoreByPlayer // SCORING FUNCTIONALITY HERE
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => nameByPlayer.TryGetValue(kv.Key, out var nm) ? nm : string.Empty, StringComparer.OrdinalIgnoreCase)
                .Take(50)
                .Select(kv => new { name = nameByPlayer.TryGetValue(kv.Key, out var nm) ? nm : "(unknown)", score = kv.Value })
                .ToArray();

            runtime.EndQuestion();

            await Clients.Group(gamePin).SendAsync("QuestionEnded", new
            {
                correctIndex,
                optionCounts = counts,
                leaderboard,
                answers = answersDto
            });

            await BroadcastSessionState(gamePin, runtime);
        }
    }
}
