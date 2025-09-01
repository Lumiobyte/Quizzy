using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;

namespace Quizzy.Core.Scoring
{
    public class FixedScoringStrategy : BaseScoringStrategy
    {

        public FixedScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {
            // calculation implementation
        }

    }
}
