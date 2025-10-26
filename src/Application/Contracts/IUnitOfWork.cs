namespace Application.Contracts
{
    /// <summary>
    /// Unit of Work pattern interface for managing database transactions and coordinating saves.
    /// Ensures all repository changes are committed as a single atomic transaction.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves all pending changes to the database.
        /// Should be called ONCE per business operation at the controller/service level.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of entities affected</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins an explicit database transaction.
        /// Use for complex operations requiring transactional integrity (e.g., multi-step workflows).
        /// </summary>
        /// <returns>Task</returns>
        Task BeginTransactionAsync();

        /// <summary>
        /// Commits the current transaction, making all changes permanent.
        /// </summary>
        /// <returns>Task</returns>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the current transaction, discarding all changes.
        /// </summary>
        /// <returns>Task</returns>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Indicates whether a transaction is currently active.
        /// </summary>
        bool HasActiveTransaction { get; }
    }
}
