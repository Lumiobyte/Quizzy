using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizPlayerRepository : Repository<QuizPlayer>, IQuizPlayerRepository
    {
        
        public QuizPlayerRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
