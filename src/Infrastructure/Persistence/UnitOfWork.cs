using Application.Contracts;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Unit of Work implementation that coordinates database operations and manages transactions.
    /// This ensures all repository changes are committed as a single atomic operation.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly TO2DbContext _dbContext;
        private IDbContextTransaction? _currentTransaction;

        public UnitOfWork(TO2DbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Indicates whether a transaction is currently active.
        /// </summary>
        public bool HasActiveTransaction => _currentTransaction != null;

        /// <summary>
        /// Saves all pending changes to the database.
        /// This is the SINGLE point where SaveChanges should be called.
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Begins an explicit database transaction.
        /// Use this for complex multi-step operations that must succeed or fail as a unit.
        /// </summary>
        public async Task BeginTransactionAsync()
        {
            if (_currentTransaction != null)
            {
                throw new InvalidOperationException("A transaction is already in progress.");
            }

            _currentTransaction = await _dbContext.Database.BeginTransactionAsync();
        }

        /// <summary>
        /// Commits the current transaction, making all changes permanent.
        /// Automatically calls SaveChanges before committing.
        /// </summary>
        public async Task CommitTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit.");
            }

            try
            {
                // Save changes before committing the transaction
                await _dbContext.SaveChangesAsync();
                await _currentTransaction.CommitAsync();
            }
            catch
            {
                // If commit fails, rollback
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }

        /// <summary>
        /// Rolls back the current transaction, discarding all changes.
        /// </summary>
        public async Task RollbackTransactionAsync()
        {
            if (_currentTransaction == null)
            {
                throw new InvalidOperationException("No active transaction to rollback.");
            }

            try
            {
                await _currentTransaction.RollbackAsync();
            }
            finally
            {
                if (_currentTransaction != null)
                {
                    await _currentTransaction.DisposeAsync();
                    _currentTransaction = null;
                }
            }
        }
    }
}
