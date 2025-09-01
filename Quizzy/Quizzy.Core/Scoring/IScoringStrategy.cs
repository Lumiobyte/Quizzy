using Quizzy.Core.Entities;

namespace Quizzy.Core.Scoring
{
    public interface IScoringStrategy
    {
        Task ScoreSessionAsync(QuizSession session);
        Task GetLeaderboardAsync(QuizSession session);
    }
}
