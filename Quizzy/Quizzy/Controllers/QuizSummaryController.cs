using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.DTOs;
using Quizzy.Core.Repositories;
using Quizzy.Core.Scoring;
using Quizzy.Core.Services;

namespace Quizzy.Web.Controllers
{
    public class QuizSummaryController(IUnitOfWork repository, IScoringStrategyFactory scoringStrategyFactory, IReportingService reportingService) : Controller
    {
        public async Task<IActionResult> Index(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("sessionId is required");
            }

            if(!Guid.TryParse(sessionId, out var sessionGuid))
            {
                return BadRequest("Invalid sessionId");
            }

            if (await repository.QuizSessions.GetByIdWithDetailsAsync(sessionGuid) is not { } session)
            {
                return BadRequest("Session not found");
            }

            IScoringStrategy scoringStrategy = scoringStrategyFactory.GetStrategy(session.ScoringStrategy);
            await scoringStrategy.ScoreSessionAsync(session);

            var leaderboardPlayers = await scoringStrategy.GetLeaderboardPlayersAsync(session);

            reportingService.SendReportsForSession(session);

            var leaderboard = new LeaderboardDto {
                QuizTitle = session.Quiz.Title,
                TotalQuestions = session.Quiz.Questions.Count,
                LeaderboardPlayers = leaderboardPlayers
            };

            return View(leaderboard);
        }
    }
}
