using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizRepository : Repository<Quiz>, IQuizRepository
    {

        public QuizRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
