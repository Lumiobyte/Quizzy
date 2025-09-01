using Microsoft.EntityFrameworkCore;
using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizQuestionRepository : Repository<QuizQuestion>, IQuizQuestionRepository
    {

        public QuizQuestionRepository(QuizzyDbContext context) : base(context) { }

        public async Task LoadQuestionAnswers(QuizQuestion question)
        {
            await _dbContext.Entry(question).Collection(q => q.Answers).LoadAsync();
        }

    }
}
