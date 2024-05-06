using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            _dbContext.Entry(entity).State = EntityState.Added;

            await _dbContext.AddAsync(entity);
            return entity;
        }

        public async Task<TEntity> Get(object id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

        public async Task<IReadOnlyList<TEntity>> GetAll()
        {
            return await _dbContext.Set<TEntity>().ToListAsync();
        }

        public async Task Update(TEntity entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
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

        public async Task Save()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
