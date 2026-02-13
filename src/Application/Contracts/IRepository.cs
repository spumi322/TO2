using Domain.Common;
using System.Linq.Expressions;

namespace Application.Contracts
{
    public interface IRepository<T> where T : EntityBase
    {
        // Query operations (nullable returns)
        Task<T?> GetByIdAsync(long id);
        Task<IReadOnlyList<T>> GetAllAsync();
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
        Task<IReadOnlyList<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(long id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);

        // Command operations (void returns for CQS)
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task DeleteAsync(T entity);
        Task DeleteAsync(long id);
    }
}
