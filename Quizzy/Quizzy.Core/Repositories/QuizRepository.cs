using Microsoft.EntityFrameworkCore;
using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class QuizRepository : Repository<Quiz>, IQuizRepository
    {

        public QuizRepository(QuizzyDbContext dbContext) : base(dbContext) { }

        public async Task<Quiz?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _dbContext.Quizzes
                .Where(q => q.Id == id) // Calling where beforehand to limit amount of db calls
                .Include(q => q.QuizAuthor)
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<List<Quiz>?> GetAllWithDetailsAsync()
        {
            return await _dbContext.Quizzes
                .Include(q => q.QuizAuthor)
                .Include(q => q.Questions)
                    .ThenInclude(qq => qq.Answers)
                .ToListAsync();
        }
    }
}
