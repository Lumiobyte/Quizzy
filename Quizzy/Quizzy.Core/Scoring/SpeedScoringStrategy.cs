using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;

namespace Quizzy.Core.Scoring
{
    public class SpeedScoringStrategy : BaseScoringStrategy
    {

        public SpeedScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {
            foreach (var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersWithQuizAnswersAsync(player);

                foreach (var correctAnswer in player.Answers.Where(a => a.Answer.IsCorrect))
                {
                    correctAnswer.PointsValue = GetTimeDecayedScore(correctAnswer.ResponseTime);
                }
            }
        }

        int GetTimeDecayedScore(TimeSpan responseTime)
        {
            int maxScore = 1100, minScore = 650;
            int maxTimeMs = 10000;
            double speedBonus = 1.5; // Higher values will reward faster answers more

            var performance = Math.Clamp(responseTime.TotalMilliseconds, 0, maxTimeMs) / maxTimeMs;
            var shaped = Math.Pow(1.0 - performance, speedBonus);

            var score = minScore + (maxScore - minScore) * shaped;

            return (int)Math.Round(Math.Clamp(score, minScore, maxScore));
        }

    }
}
