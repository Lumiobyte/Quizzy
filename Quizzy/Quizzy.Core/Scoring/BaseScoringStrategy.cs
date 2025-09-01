using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;

namespace Quizzy.Core.Scoring
{
    public abstract class BaseScoringStrategy : IScoringStrategy
    {

        protected readonly IUnitOfWork _unitOfWork;

        public BaseScoringStrategy(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public virtual async Task GetLeaderboardAsync(QuizSession session)
        {
            // calculate lb based on scores
            // return KVP of players + their score.
        }

        public async Task ScoreSessionAsync(QuizSession session)
        {
            if (session.ScoringComplete)
            {
                return;
            }

            await DoScoreSessionAsync(session);

            session.ScoringComplete = true;
            await _unitOfWork.SaveChangesAsync();
        }

        protected abstract Task DoScoreSessionAsync(QuizSession session);

    }
}
