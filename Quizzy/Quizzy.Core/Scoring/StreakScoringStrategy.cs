using Quizzy.Core.Entities;
using Quizzy.Core.Repositories;
using System.Numerics;

namespace Quizzy.Core.Scoring
{
    public class StreakScoringStrategy : BaseScoringStrategy
    {

        public StreakScoringStrategy(IUnitOfWork unitOfWork) : base(unitOfWork) { }

        protected override async Task DoScoreSessionAsync(QuizSession session)
        {
            await _unitOfWork.Quizzes.LoadQuizQuestionsAsync(session.Quiz);

            foreach(var player in session.Players)
            {
                await _unitOfWork.QuizPlayers.LoadPlayerAnswersWithQuizAnswersAsync(player);

                var orderedAnswers = session.QuestionOrderList // For every OrderIndex, ordered according to this session's question order...
                    .Select(oi => player.Answers // Get the PlayerAnswer where...
                        .First(a => session.Quiz.Questions // The QuizQuestion this PlayerAnswer is answering...
                            .First(q => q.Id == a.Answer.QuestionId).OrderIndex == oi // Has OrderIndex equal to the target order index 'oi'
                        )
                    ).ToList();
                // The end result is a list of answers that follow the order of the questions as they were shown in the actual quiz
                // Since DB retrieval/IEnumerable does not guarantee order, we need to recalculate this list to ensure correct scoring

                ScoreAnswers(orderedAnswers, session.Quiz.Questions.Count);
            }
        }

        void ScoreAnswers(List<PlayerAnswer> answers, int totalQuestions)
        {
            int streak = 0;
            foreach (var answer in answers)
            {
                if (answer.Answer.IsCorrect)
                {
                    streak++;
                    answer.PointsValue = GetStreakBonusedScore(streak, totalQuestions);
                }
                else
                {
                    streak = 0;
                }
            }
        }

        int GetStreakBonusedScore(int streak, int totalQuestions)
        {
            int maxScore = 1600, minScore = 1000;

            var performance = (double)streak / totalQuestions;
            var shaped = (1.0 - Math.Cos(performance * Math.PI)) / 2;

            var score = minScore + (maxScore - minScore) * shaped;

            return (int)Math.Round(score);
        }

    }
}
