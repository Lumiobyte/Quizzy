using Quizzy.Core.DTOs;
using Quizzy.Core.Entities;
using Quizzy.Core.Enums;
using Quizzy.Core.Repositories;
using System.Numerics;

namespace Quizzy.Core.Scoring
{
    public abstract class BaseScoringStrategy : IScoringStrategy
    {

        protected readonly IUnitOfWork _unitOfWork;

        public BaseScoringStrategy(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public virtual async Task<List<LeaderboardPlayerDto>> GetLeaderboardPlayersAsync(QuizSession session)
        {
            foreach(var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersAsync(player);
            }

            session.Players.OrderBy(p => p.TotalScore);

            // return KVP of players + their score.
        }

        public virtual async Task CalculatePlayerTotalsAsync(QuizSession session)
        {
            foreach(var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersAsync(player);

                player.TotalScore = player.Answers.Sum(a => a.PointsValue);
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
