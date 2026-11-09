using Application.Contracts;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Postgres
{
    /// <summary>
    /// One throwaway Postgres container (same image as docker-compose) shared by the Postgres tests.
    /// Each test gets its own empty database, so tests don't depend on each other or on run order.
    /// </summary>
    public class PostgresFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
            .Build();

        public Task InitializeAsync() => _container.StartAsync();

        public Task DisposeAsync() => _container.DisposeAsync().AsTask();

        /// <summary>
        /// A context on a new, not yet created database (Migrate creates it). Interceptors mirror Program.cs.
        /// </summary>
        public TO2DbContext CreateContextOnNewDatabase()
        {
            var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Database = $"to2_{Guid.NewGuid():N}"
            }.ConnectionString;

            var tenantService = new Mock<ITenantService>();
            tenantService.Setup(x => x.GetCurrentTenantId()).Returns(1);
            tenantService.Setup(x => x.GetCurrentUserName()).Returns("TestUser");

            var options = new DbContextOptionsBuilder<TO2DbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(
                    new TenantSaveChangesInterceptor(tenantService.Object),
                    new ConcurrencyVersionInterceptor())
                .Options;

            return new TO2DbContext(options, new ConfigurationBuilder().Build(), tenantService.Object);
        }
    }

    [CollectionDefinition(Name)]
    public class PostgresCollection : ICollectionFixture<PostgresFixture>
    {
        public const string Name = "Postgres";
    }
}
