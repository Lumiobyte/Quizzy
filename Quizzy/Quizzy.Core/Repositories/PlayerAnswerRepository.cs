using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class PlayerAnswerRepository : Repository<PlayerAnswer>, IPlayerAnswerRepository
    {

        public PlayerAnswerRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
