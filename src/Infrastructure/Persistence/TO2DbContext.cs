using Application.Contracts;
using Application.Services.EventHandling;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public class TO2DbContext : DbContext, ITO2DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly IDomainEventDispatcher _eventDispatcher;

        public TO2DbContext()
        {
        }

        public TO2DbContext(DbContextOptions<TO2DbContext> options, IConfiguration configuration, IDomainEventDispatcher eventDispatcher)
            : base(options)
        {
            _configuration = configuration;
            _eventDispatcher = eventDispatcher;
        }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Standing> Standings { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<TeamsTournaments> TeamsTournaments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlite(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Not Implemented, should be ignored
            modelBuilder.Ignore<Prize>();

            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tournament).Assembly);

            modelBuilder.Entity<Tournament>()
                .HasIndex(e => e.Name)
                .IsUnique();

            modelBuilder.Entity<TeamsTournaments>()
                .HasKey(tt => new { tt.TeamId, tt.TournamentId });

            modelBuilder.Entity<TeamsTournaments>()
                .HasOne(tt => tt.Team)
                .WithMany(t => t.TeamsTournaments)
                .HasForeignKey(tt => tt.TeamId);

            modelBuilder.Entity<TeamsTournaments>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.TeamsTournaments)
                .HasForeignKey(tt => tt.TournamentId);
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();

            var domainEntities = ChangeTracker.Entries<EntityBase>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity)
                .ToList();

            int result = await base.SaveChangesAsync(cancellationToken);

            foreach (var entity in domainEntities)
            {
                foreach (var domainEvent in entity.DomainEvents)
                {
                    await _eventDispatcher.DispatchAsync(domainEvent);
                }
                entity.ClearEvents();
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
