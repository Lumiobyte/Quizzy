using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizAnswerRepository : Repository<QuizAnswer>, IQuizAnswerRepository
    {

        public QuizAnswerRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
