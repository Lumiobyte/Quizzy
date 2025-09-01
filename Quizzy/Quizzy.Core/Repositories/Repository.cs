using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Quizzy.Core.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {

        protected readonly QuizzyDbContext _dbContext;
        private readonly DbSet<T> _dbSet;

        public Repository(QuizzyDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            _dbSet.Attach(entity);
            _dbContext.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Remove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public virtual async Task Remove(Guid id)
        {
            if(await _dbSet.FindAsync(id) is { } target)
            {
                _dbSet.Remove(target);
            }
        }

    }
}
