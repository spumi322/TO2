using Domain.Common;
using Domain.Entities;
using Domain.Enums;

namespace Domain.AggregateRoots
{
    public class Tournament : AggregateRootBase
    {
        private readonly List<Standing> _standings = new();

        private Tournament() { }

        public Tournament(string name, string? description, int maxTeams, Format format)
        {
            Name = name;
            Description = description;
            MaxTeams = maxTeams;
            Format = format;
        }

        public string Name { get; private set; }

        public string? Description { get; set; }

        public int MaxTeams { get; set; }

        public Format Format { get; set; }

        public TournamentStatus Status { get; set; } = TournamentStatus.Setup;

        public int? AdvancingPerGroup { get; set; }

        public DateTime? CancelledAt { get; private set; }

        public string? CancelledBy { get; private set; }

        public string? CancellationReason { get; private set; }

        public long TenantId { get; private set; }
        public Tenant Tenant { get; set; } = null!;

        public ICollection<Team> Teams { get; private set; } = new List<Team>();

        public IReadOnlyList<Standing> Standings => _standings;

        public void UpdateDetails(string name, string? description)
        {
            Name = name;
            Description = description;
        }

        // Caller must check ITournamentStateMachine.CanCancel first
        public void Cancel(string cancelledBy, DateTime cancelledAt, string? reason)
        {
            Status = TournamentStatus.Cancelled;
            CancelledBy = cancelledBy;
            CancelledAt = cancelledAt;
            CancellationReason = reason;
        }
    }
}
