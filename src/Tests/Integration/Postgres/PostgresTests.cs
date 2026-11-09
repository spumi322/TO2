using Domain.AggregateRoots;
using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration.Postgres
{
    /// <summary>
    /// What SQLite can't show, on a real Postgres (needs Docker; skip locally with --filter "Category!=Postgres").
    /// </summary>
    [Collection(PostgresCollection.Name)]
    [Trait("Category", "Postgres")]
    public class PostgresTests
    {
        private readonly PostgresFixture _postgres;

        public PostgresTests(PostgresFixture postgres)
        {
            _postgres = postgres;
        }

        [Fact]
        public async Task Migrations_ApplyToEmptyDatabase()
        {
            await using var context = _postgres.CreateContextOnNewDatabase();

            await context.Database.MigrateAsync();

            (await context.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
            (await context.Database.GetAppliedMigrationsAsync()).Should().Equal(context.Database.GetMigrations());
        }

        [Fact]
        public async Task DuplicateOrganizationName_IsConflict()
        {
            await using var context = _postgres.CreateContextOnNewDatabase();
            await context.Database.MigrateAsync();
            var unitOfWork = new UnitOfWork(context);

            context.Tenants.Add(new Tenant { Name = "Acme" });
            await unitOfWork.SaveChangesAsync();

            // Same name ignoring case and spaces: the unique NormalizedName index rejects it (Postgres SQL state 23505)
            context.Tenants.Add(new Tenant { Name = " acme " });
            var save = () => unitOfWork.SaveChangesAsync();

            await save.Should().ThrowAsync<ConflictException>();
        }
    }
}
