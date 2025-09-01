using Quizzy.Core.Entities;

namespace Quizzy.Core.Scoring
{
    public interface IScoringStrategy
    {
        void ScoreAnswers(List<PlayerAnswer> answers);
        void GetLeaderboard(QuizSession session);
    }
}
