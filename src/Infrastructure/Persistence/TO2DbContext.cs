using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class TO2DbContext : DbContext, ITO2DbContext
    {
        private readonly IConfiguration _configuration;

        public TO2DbContext()
        {
        }

        public TO2DbContext(DbContextOptions<TO2DbContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Standing> Standings { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Group> GroupEntries { get; set; }
        public DbSet<TournamentTeam> TournamentTeams { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured && _configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseSqlite(connectionString);
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<AggregateRootBase>();
            modelBuilder.Ignore<EntityBase>();
            modelBuilder.Ignore<ValueObjectBase>();
            modelBuilder.Ignore<Prize>();

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TournamentTeam>(entity =>
            {
                entity.HasKey(tt => new { tt.TournamentId, tt.TeamId });

                entity.HasOne(tt => tt.Tournament)
                    .WithMany(t => t.TournamentTeams)
                    .HasForeignKey(tt => tt.TournamentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(tt => tt.Team)
                    .WithMany(t => t.TournamentParticipations)
                    .HasForeignKey(tt => tt.TeamId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Ignore(tt => tt.Id);
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tournament).Assembly);

            modelBuilder.Entity<Tournament>()
                .HasIndex(e => e.Name)
                .IsUnique();


        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();

            int result = await base.SaveChangesAsync(cancellationToken);

            // Save again if there are changes
            if (ChangeTracker.HasChanges())
            {
                AddTimestamps();
                result = await base.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        private void AddTimestamps()
        {
            var userName = "PlaceHolder";

            var entries = ChangeTracker.Entries()
                .Where(x => x.Entity is EntityBase && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var entity = (EntityBase)entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                    entity.CreatedBy = userName;
                }

                entity.LastModifiedDate = DateTime.UtcNow;
                entity.LastModifiedBy = userName;
            }
        }
    }
}
