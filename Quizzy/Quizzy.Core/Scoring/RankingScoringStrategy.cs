using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;

namespace Quizzy.Core.Scoring
{
    public class RankingScoringStrategy : BaseScoringStrategy
    {
        public RankingScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {

            List<PlayerAnswer> allAnswers = new List<PlayerAnswer>();
            foreach (var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersWithQuizAnswersAsync(player);
                allAnswers.AddRange(player.Answers);
            }

            await _unitOfWork.Quizzes.LoadQuizQuestionsAsync(session.Quiz);

            foreach(var question in session.Quiz.Questions)
            {
                ScoreAnswers(allAnswers.Where(a => a.QuestionId == question.Id).OrderBy(a => a.ResponseTime.TotalMilliseconds).ToList(), session.Players.Count);
            }
        }

        void ScoreAnswers(List<PlayerAnswer> playerAnswers, int totalPlayers)
        {
            int rank = 1;
            foreach (var correctAnswer in playerAnswers.Where(a => a.Answer.IsCorrect))
            {
                correctAnswer.PointsValue = GetRankDecayedScore(rank, totalPlayers);
                rank++;
            }
        }

        int GetRankDecayedScore(int rank, int totalPlayers)
        {
            int maxScore = 1000, minScore = 700;
            double rankBonus = 1.0; // Higher values favour earlier answers

            var performance = (double)rank / totalPlayers;
            var shaped = 1.0 - Math.Pow(performance, rankBonus);

            var score = minScore + (maxScore - minScore) * shaped;

            return (int)Math.Round(score);
        }

    }
}
