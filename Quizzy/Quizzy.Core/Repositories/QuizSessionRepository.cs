using Microsoft.EntityFrameworkCore;
using Quizzy.Core.Entities;
using Quizzy.Core.Extensions;

namespace Quizzy.Core.Repositories
{
    public class QuizSessionRepository : Repository<QuizSession>, IQuizSessionRepository
    {

        public QuizSessionRepository(QuizzyDbContext context) : base(context) { }

        public async Task<QuizSession?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbContext.QuizSessions
                .Where(s => s.Id == id)
                .Include(s => s.QuizHost)
                .Include(s => s.Players)
                .Include(s => s.Quiz)
                    .ThenInclude(sq => sq.Questions)
                .FirstOrDefaultAsync();
        }

        public QuizQuestion? GetFirstQuestion(QuizSession session)
        {
            return session.Quiz.Questions.FirstOrDefault(q => q.OrderIndex == session.QuestionOrderList.First());
        }

        public QuizQuestion? GetNextQuestion(QuizSession session, int currentOrderIndex)
        {
            return session.Quiz.Questions.FirstOrDefault(q => q.OrderIndex == GetNextQuestionIndex(currentOrderIndex, session.QuestionOrderList));
        }

        public override async Task AddAsync(QuizSession session)
        {
            if (string.IsNullOrWhiteSpace(session.QuestionOrder))
            {
                session.QuestionOrder = session.Quiz.GetQuestionOrder();
            }

            await base.AddAsync(session);
        }

        private int GetNextQuestionIndex(int currentQuestionIndex, List<int> order)
        {
            var next = order.IndexOf(currentQuestionIndex) + 1;

            if(order.Count - 1 > next)
            {
                return order[next];
            }

            return -1;
        }
    }
}
