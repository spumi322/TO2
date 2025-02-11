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
        public DbSet<TournamentParticipants> TeamsTournaments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            //var connectionString = _configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlite("Data Source=G:\\Code\\TO2\\src\\Infrastructure\\app.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<DomainEvent>();
            modelBuilder.Ignore<AggregateRootBase>();
            modelBuilder.Ignore<EntityBase>();
            modelBuilder.Ignore<ValueObjectBase>();
            modelBuilder.Ignore<Prize>();

            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tournament).Assembly);

            modelBuilder.Entity<Tournament>()
                .HasIndex(e => e.Name)
                .IsUnique();

            modelBuilder.Entity<TournamentParticipants>()
                .HasKey(tp => new { tp.TeamId, tp.TournamentId });

            modelBuilder.Entity<TournamentParticipants>()
                .HasOne(tt => tt.Team)
                .WithMany(t => t.TournamentParticipants)
                .HasForeignKey(tt => tt.TeamId);

            modelBuilder.Entity<TournamentParticipants>()
                .HasOne(tt => tt.Tournament)
                .WithMany(t => t.TournamentParticipants)
                .HasForeignKey(tt => tt.TournamentId);

            modelBuilder.Entity<TournamentParticipants>()
                .HasOne(tp => tp.Standing)
                .WithMany(s => s.TournamentParticipants)
                .HasForeignKey(tp => tp.StandingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TournamentParticipants>()
                .Property(tp => tp.Status)
                .HasConversion<int>();

            modelBuilder.Entity<TournamentParticipants>()
                .Property(tp => tp.Eliminated)
                .HasDefaultValue(false);
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
