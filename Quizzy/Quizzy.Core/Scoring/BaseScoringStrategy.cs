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

            var orderedPlayers = session.Players.OrderByDescending(p => p.TotalScore);

            var leaderboard = new List<LeaderboardPlayerDto>();
            int rank = 1;

            foreach(var player in orderedPlayers)
            {
                leaderboard.Add(new LeaderboardPlayerDto {
                    PlayerName = player.Name,
                    PlayerRanking = rank,
                    PlayerScore = player.TotalScore
                });
                rank++;
            }

            return leaderboard;
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
