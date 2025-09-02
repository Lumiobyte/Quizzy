using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using Quizzy.Core.Enums;

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

        public virtual async Task CalculatePlayerTotalsAsync(QuizSession session)
        {
            foreach(var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersAsync(player);

                int total = player.Answers.Sum(a => a.PointsValue);

                player.TotalScore = total;
            }
        }

        public async Task ScoreSessionAsync(QuizSession session)
        {
            if (session.State is QuizState.InProgress || session.ScoringComplete)
            {
                return;
            }

            await DoScoreSessionAsync(session);
            await CalculatePlayerTotalsAsync(session);

            session.ScoringComplete = true;
            await _unitOfWork.SaveChangesAsync();
        }

        protected abstract Task DoScoreSessionAsync(QuizSession session);

    }
}
