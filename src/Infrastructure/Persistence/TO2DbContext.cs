using Application.Contracts;
using Domain.AggregateRoots;
using Domain.Common;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public class TO2DbContext : IdentityDbContext<User, IdentityRole<long>, long>
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
        public DbSet<Standing> Standings { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<GroupEntry> GroupEntries { get; set; }
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

            base.OnModelCreating(modelBuilder);

            // App-managed optimistic concurrency (incremented by ConcurrencyVersionInterceptor)
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(t => typeof(EntityBase).IsAssignableFrom(t.ClrType)))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(EntityBase.Version))
                    .IsConcurrencyToken();
            }

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.Property(t => t.NormalizedName).IsRequired().HasMaxLength(100);
                entity.Property(t => t.InviteCodeHash).HasMaxLength(64);

                // Names are unique case-insensitively; the index also stops two simultaneous registrations creating the same name
                entity.HasIndex(t => t.NormalizedName).IsUnique();

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
                entity.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(64);
                entity.HasIndex(rt => rt.TokenHash).IsUnique();
                entity.HasOne(rt => rt.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // A team belongs to one tournament and is deleted with it
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Tournament)
                .WithMany(t => t.Teams)
                .HasForeignKey(t => t.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Team>().Property(t => t.Name).HasMaxLength(24);
            modelBuilder.Entity<Tournament>().Property(t => t.Name).HasMaxLength(60);

            modelBuilder.Entity<Tournament>().HasQueryFilter(t => t.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Team>().HasQueryFilter(t => t.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Match>().HasQueryFilter(m => m.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Standing>().HasQueryFilter(s => s.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<GroupEntry>().HasQueryFilter(g => g.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Game>().HasQueryFilter(g => g.TenantId == GetCurrentTenantId());

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Tournament).Assembly);

        }

        /// <summary>
        /// Gets the current tenant ID for query filters.
        /// New entities get TenantId from TenantSaveChangesInterceptor (not from here).
        /// </summary>
        private long GetCurrentTenantId()
        {
            if (_tenantService == null)
                throw new InvalidOperationException("TenantService is not available");

            return _tenantService.GetCurrentTenantId();
        }
    }
}
