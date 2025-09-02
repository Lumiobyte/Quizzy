using Quizzy.Core.DTOs;
using Quizzy.Core.Entities;

namespace Quizzy.Core.Scoring
{
    public interface IScoringStrategy
    {
        Task ScoreSessionAsync(QuizSession session);
        Task<List<LeaderboardPlayerDto>> GetLeaderboardPlayersAsync(QuizSession session);
    }
}
