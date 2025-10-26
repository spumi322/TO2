namespace Application.Contracts
{
    public interface IGenericRepository<T>
    {
        Task<T> Add(T entity);
        Task AddRange(IEnumerable<T> entities);
        Task<T> Get(object id);
        Task<IReadOnlyList<T>> GetAll();
        Task<IReadOnlyList<T>> GetAllByFK(string foreignKeyName, long foreignKeyId);
        Task Update(T entity);
        Task Delete(object id);
        Task<bool> Exists(object id);
    }
}
