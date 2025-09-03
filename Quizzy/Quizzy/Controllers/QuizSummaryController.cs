using Microsoft.AspNetCore.Mvc;
using Quizzy.Core.DTOs;
using Quizzy.Core.Repositories;
using Quizzy.Core.Scoring;

namespace Quizzy.Web.Controllers
{
    public class QuizSummaryController(IUnitOfWork repository, IScoringStrategyFactory scoringStrategyFactory) : Controller
    {
        public async Task<IActionResult> Index(string pin)
        { 
            if (await repository.QuizSessions.GetByPinWithDetailsAsync(pin) is not { } session)
            {
                return BadRequest("Session not found");
            }

            IScoringStrategy scoringStrategy = scoringStrategyFactory.GetStrategy(session.ScoringStrategy);
            await scoringStrategy.ScoreSessionAsync(session);

            var leaderboardPlayers = await scoringStrategy.GetLeaderboardPlayersAsync(session);

            var leaderboard = new LeaderboardDto {
                QuizTitle = session.Quiz.Title,
                TotalQuestions = session.Quiz.Questions.Count,
                LeaderboardPlayers = leaderboardPlayers
            };

            return View(leaderboard);
        }
    }
}
