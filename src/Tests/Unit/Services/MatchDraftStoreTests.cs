using Application.DTOs.SignalR;
using Application.Services;
using FluentAssertions;

namespace Tests.Unit.Services
{
    public class MatchDraftStoreTests
    {
        private sealed class FakeTime : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => Now;
        }

        private readonly FakeTime _time = new();
        private readonly MatchDraftStore _store;

        public MatchDraftStoreTests()
        {
            _store = new MatchDraftStore(_time);
        }

        private static MatchDraftUpdatedEvent Draft(long tournamentId, long matchId, int a, int b) =>
            new(tournamentId, matchId, a, b, "TestUser");

        [Fact]
        public void Should_KeepLatestDraftPerMatch_OfThatTournamentOnly()
        {
            _store.Set(Draft(1, 10, 1, 0));
            _store.Set(Draft(1, 10, 1, 1));
            _store.Set(Draft(1, 11, 0, 1));
            _store.Set(Draft(2, 20, 1, 0));

            _store.GetAll(1).Should().BeEquivalentTo(new[] { Draft(1, 10, 1, 1), Draft(1, 11, 0, 1) });
        }

        [Fact]
        public void Should_RemoveDraft_When_SetBackToZeroZero()
        {
            _store.Set(Draft(1, 10, 1, 0));

            _store.Set(Draft(1, 10, 0, 0));

            _store.GetAll(1).Should().BeEmpty();
        }

        [Fact]
        public void Should_RemoveDraft_When_Removed()
        {
            _store.Set(Draft(1, 10, 1, 0));
            _store.Set(Draft(1, 11, 1, 0));

            _store.Remove(1, 10);

            _store.GetAll(1).Should().ContainSingle().Which.MatchId.Should().Be(11);
        }

        [Fact]
        public void Should_DropDraft_When_OlderThanMaxAge()
        {
            _store.Set(Draft(1, 10, 1, 0));
            _time.Now += MatchDraftStore.MaxAge - TimeSpan.FromMinutes(1);
            _store.Set(Draft(1, 11, 1, 0));

            _time.Now += TimeSpan.FromMinutes(2);

            _store.GetAll(1).Should().ContainSingle().Which.MatchId.Should().Be(11);
        }
    }
}
