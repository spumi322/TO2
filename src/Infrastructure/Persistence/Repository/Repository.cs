using Application.Contracts;
using Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Infrastructure.Persistence.Repository
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : EntityBase
    {
        protected readonly TO2DbContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;

        public Repository(TO2DbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = dbContext.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync(long id)
          => await _dbSet.FindAsync(id);

        public async Task<IReadOnlyList<TEntity>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate);

        public async Task<IReadOnlyList<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public async Task<bool> ExistsAsync(long id)
            => await _dbSet.FindAsync(id) != null;

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        public async Task<int> CountAsync()
            => await _dbSet.CountAsync();

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
            => await _dbSet.CountAsync(predicate);

        public async Task AddAsync(TEntity entity)
            => await _dbSet.AddAsync(entity);

        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
            => await _dbSet.AddRangeAsync(entities);

        public Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(long id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null) _dbSet.Remove(entity);
        }
    }
}
