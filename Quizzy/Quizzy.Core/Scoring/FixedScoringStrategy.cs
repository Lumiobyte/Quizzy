using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System.Numerics;

namespace Quizzy.Core.Scoring
{
    public class FixedScoringStrategy : BaseScoringStrategy
    {

        public FixedScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {
            foreach(var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersWithQuizAnswersAsync(player);

                foreach (var correctAnswer in player.Answers.Where(a => a.Answer.IsCorrect))
                {
                    correctAnswer.PointsValue = 1000;
                }
            }
        }

    }
}
