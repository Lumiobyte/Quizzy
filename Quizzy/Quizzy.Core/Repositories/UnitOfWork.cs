using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quizzy.Core.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {

        private readonly QuizzyDbContext _dbContext;

        public UnitOfWork(QuizzyDbContext dbContext) { 
            _dbContext = dbContext;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // If there is any code we would like to always run before writing to the DB, we can call those functions here!
            // E.g. some kind of validation, logging, counters etc. If you not sure if this is the right place for it, ask Evan

            return _dbContext.SaveChangesAsync(cancellationToken);
        }

    }
}
