using Microsoft.EntityFrameworkCore;
using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizPlayerRepository : Repository<QuizPlayer>, IQuizPlayerRepository
    {
        
        public QuizPlayerRepository(QuizzyDbContext dbContext) : base(dbContext) { }

        public async Task LoadPlayerAnswers(QuizPlayer player)
        {
            await _dbContext.Entry(player).Collection(p => p.Answers).LoadAsync();
        }

        public async Task LoadPlayerAnswersWithQuizAnswers(QuizPlayer player)
        {
            await _dbContext.Entry(player).Collection(p => p.Answers).Query().Include(a => a.Answer).LoadAsync();
        }

    }
}
