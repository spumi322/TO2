using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class TO2DbContext : IdentityDbContext<User, IdentityRole<long>, long>, ITO2DbContext
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantService? _tenantService;

        public TO2DbContext()
        {
        }

        public TO2DbContext(
            DbContextOptions<TO2DbContext> options,
            IConfiguration configuration,
            ITenantService tenantService)
            : base(options)
        {
            _configuration = configuration;
            _tenantService = tenantService;
        }

        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Standing> Standings { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Group> GroupEntries { get; set; }
        public DbSet<TournamentTeam> TournamentTeams { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            if (!optionsBuilder.IsConfigured && _configuration != null)
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    optionsBuilder.UseNpgsql(connectionString);
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

            modelBuilder.Entity<Tournament>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Match>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Game>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Standing>().Property(e => e.RowVersion).IsRowVersion();

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.HasMany(t => t.Users)
                      .WithOne(u => u.Tenant)
                      .HasForeignKey(u => u.TenantId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(u => u.UserName).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                entity.Property(rt => rt.Token).IsRequired();
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

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

            modelBuilder.Entity<Tournament>().HasQueryFilter(t => t.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Team>().HasQueryFilter(t => t.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Match>().HasQueryFilter(m => m.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Standing>().HasQueryFilter(s => s.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Group>().HasQueryFilter(g => g.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Game>().HasQueryFilter(g => g.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<TournamentTeam>().HasQueryFilter(tt => tt.TenantId == GetCurrentTenantId());

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tournament).Assembly);

            modelBuilder.Entity<Tournament>()
                .HasIndex(e => e.Name)
                .IsUnique();


        }

        /// <summary>
        /// Gets the current tenant ID for query filters.
        /// Note: TenantId is now auto-set on new entities by TenantSaveChangesInterceptor,
        /// and audit fields are set by AuditInterceptor.
        /// </summary>
        private long GetCurrentTenantId()
        {
            if (_tenantService == null)
                throw new InvalidOperationException("TenantService is not available");

            return _tenantService.GetCurrentTenantId();
        }
    }
}
