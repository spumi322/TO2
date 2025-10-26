using Application.Contracts;
using Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repository
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : EntityBase
    {
        private readonly TO2DbContext _dbContext;

        public GenericRepository(TO2DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TEntity> Add(TEntity entity)
        {
            await _dbContext.AddAsync(entity);
            return entity;
        }

        public async Task AddRange(IEnumerable<TEntity> entities)
        {
            await _dbContext.AddRangeAsync(entities);
        }

        public async Task<TEntity> Get(object id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        public async Task<IReadOnlyList<TEntity>> GetAll()
        {
            return await _dbContext.Set<TEntity>().ToListAsync();
        }

        public async Task<IReadOnlyList<TEntity>> GetAllByFK(string foreignKeyName, long foreignKeyId)
        {
            return await _dbContext.Set<TEntity>()
                .Where(e => EF.Property<long>(e, foreignKeyName) == foreignKeyId)
                .ToListAsync();
        }

        public Task Update(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public async Task Delete(object id)
        {
            var entity = await _dbContext.FindAsync<TEntity>(id);

            if (entity is not null)
            {
                _dbContext.Remove(entity);
            }
        }

        public async Task<bool> Exists(object id)
        {
            var entity = await Get(id);
            return entity is not null;
        }
    }
}
