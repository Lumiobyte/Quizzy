using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizSessionRepository : Repository<QuizSession>, IQuizSessionRepository
    {

        public QuizSessionRepository(QuizzyDbContext context) : base(context) { }

    }
}
