using Infrastructure.Persistence;
using Tests.Helpers;

namespace Tests.Base
{
    /// <summary>
    /// Base class for integration tests that require database access.
    /// Provides a fresh in-memory database for each test.
    /// </summary>
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly TestDbContextFactory DbFactory;
        protected readonly TO2DbContext DbContext;

        protected IntegrationTestBase()
        {
            DbFactory = new TestDbContextFactory();
            DbContext = DbFactory.CreateContext();
        }

        /// <summary>
        /// Saves changes and detaches all entities to simulate a fresh context.
        /// Use this to test repository methods that query the database.
        /// </summary>
        protected async Task SaveAndDetachAsync()
        {
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
            DbFactory?.Dispose();
        }
    }
}
