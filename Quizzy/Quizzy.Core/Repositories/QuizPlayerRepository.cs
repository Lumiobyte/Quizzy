using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Repositories
{
    public class QuizPlayerRepository : Repository<QuizPlayer>, IQuizPlayerRepository
    {
        
        public QuizPlayerRepository(QuizzyDbContext dbContext) : base(dbContext) { }

    }
}
