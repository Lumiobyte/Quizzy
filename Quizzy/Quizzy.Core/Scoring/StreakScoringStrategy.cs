using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;

namespace Quizzy.Core.Scoring
{
    public class StreakScoringStrategy : BaseScoringStrategy
    {

        public StreakScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {
            // calculation implementation
        }

    }
}
