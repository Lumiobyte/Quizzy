using Quizzy.Core.Entities;

namespace Quizzy.Core.Repositories
{
    public class UserAccountRepository : Repository<UserAccount>, IUserAccountRepository
    {

        public UserAccountRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
