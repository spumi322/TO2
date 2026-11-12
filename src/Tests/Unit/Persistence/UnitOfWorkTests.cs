using Domain.AggregateRoots;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tests.Base;

namespace Tests.Unit.Persistence
{
    public class UnitOfWorkTests : IntegrationTestBase
    {
        private readonly UnitOfWork _unitOfWork;

        public UnitOfWorkTests()
        {
            _unitOfWork = new UnitOfWork(DbContext);

            DbContext.Tenants.Add(new Tenant { Id = 1, Name = "Org1" });
            DbContext.SaveChanges();
        }

        [Fact]
        public async Task Rollback_Should_DoNothing_When_NoActiveTransaction()
        {
            var act = () => _unitOfWork.RollbackTransactionAsync();

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Should_SurfaceCommitError_When_CallerRollsBackAfterFailedCommit()
        {
            // Same try/catch shape as the pipelines: a failed commit already rolled back,
            // so the caller's rollback must not replace the original exception
            await _unitOfWork.BeginTransactionAsync();
            DbContext.GroupEntries.Add(new GroupEntry(999, 999, 999, "Ghost")); // FK violation

            var act = async () =>
            {
                try
                {
                    await _unitOfWork.CommitTransactionAsync();
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            };

            await act.Should().ThrowAsync<DbUpdateException>();
            _unitOfWork.HasActiveTransaction.Should().BeFalse();
        }
    }
}
