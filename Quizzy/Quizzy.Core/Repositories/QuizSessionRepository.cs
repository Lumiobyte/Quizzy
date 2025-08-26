using Quizzy.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Repositories
{
    public class QuizSessionRepository : Repository<QuizSession>, IQuizSessionRepository
    {

        public QuizSessionRepository(QuizzyDbContext context) : base(context) { }

    }
}
