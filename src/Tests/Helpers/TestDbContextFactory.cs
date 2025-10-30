using Application.Contracts;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Tests.Helpers
{
    /// <summary>
    /// Factory for creating in-memory test database contexts.
    /// Uses SQLite in-memory mode for fast, isolated integration tests.
    /// </summary>
    public class TestDbContextFactory : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<TO2DbContext> _options;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;

        public TestDbContextFactory()
        {
            // Create a mock configuration (TO2DbContext needs it but doesn't use it in tests)
            var mockConfiguration = new Mock<IConfiguration>();
            _configuration = mockConfiguration.Object;

            // Create a mock tenant service for tests
            var mockTenantService = new Mock<ITenantService>();
            mockTenantService.Setup(x => x.GetCurrentTenantId()).Returns(1L);
            mockTenantService.Setup(x => x.GetCurrentUserName()).Returns("TestUser");
            _tenantService = mockTenantService.Object;

            // Create SQLite in-memory connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Configure DbContext to use in-memory SQLite
            _options = new DbContextOptionsBuilder<TO2DbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create database schema
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// Creates a new DbContext instance with the in-memory database.
        /// </summary>
        public TO2DbContext CreateContext()
        {
            return new TO2DbContext(_options, _configuration, _tenantService);
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
