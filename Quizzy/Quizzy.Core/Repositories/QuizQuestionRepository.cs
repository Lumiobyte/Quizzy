using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizQuestionRepository : Repository<QuizQuestion>, IQuizQuestionRepository
    {

        public QuizQuestionRepository(QuizzyDbContext context) : base(context) { }

    }
}
