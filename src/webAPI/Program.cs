using Application.Contracts;
using Application.Contracts.Repositories;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Strategies;
using Application.Pipelines.StartBracket;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartBracket.Strategies;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using Application.Services;
using Domain.Entities;
using Domain.StateMachine;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Repository;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using TO2.Hubs;
using TO2.SignalR;
using webAPI.Middleware;

namespace TO2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<TO2DbContext>((serviceProvider, options) =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(
                           serviceProvider.GetRequiredService<TenantSaveChangesInterceptor>(),
                           serviceProvider.GetRequiredService<AuditInterceptor>(),
                           serviceProvider.GetRequiredService<ConcurrencyVersionInterceptor>()
                       );
            });

            // UoW, Repos
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
            builder.Services.AddScoped<IGroupRepository, GroupRepository>();
            builder.Services.AddScoped<IStandingRepository, StandingRepository>();
            builder.Services.AddScoped<IMatchRepository, MatchRepository>();
            builder.Services.AddScoped<ITournamentTeamRepository, TournamentTeamRepository>();

            // Domain Services
            builder.Services.AddScoped<ITournamentStateMachine, TournamentStateMachine>();
            builder.Services.AddScoped<IFormatService, FormatService>();

            // Multi-Tenancy Service
            builder.Services.AddScoped<ITenantService, HttpContextTenantService>();
            builder.Services.AddSignalR();
            builder.Services.AddScoped<ISignalRService, SignalRService>();

            // EF Core interceptors: tenant isolation, auditing, concurrency versions
            builder.Services.AddScoped<TenantSaveChangesInterceptor>();
            builder.Services.AddScoped<AuditInterceptor>();
            builder.Services.AddScoped<ConcurrencyVersionInterceptor>();

            // Application Services
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IIdentityService, IdentityService>();
            builder.Services.AddScoped<ITokenService, JwtTokenService>();

            // Workflow orchestration (delegates to the pipelines below)
            builder.Services.AddScoped<IWorkFlowService, WorkFlowService>();
            // Game Result Pipeline - SOLID refactoring of ProcessGameResult
            builder.Services.AddScoped<IGameResultPipeline, GameResultPipeline>();
            // Pipeline Steps (order defined in GameResultPipeline.StepOrder)
            foreach (var step in GameResultPipeline.StepOrder)
                builder.Services.AddScoped(typeof(IGameResultPipelineStep), step);
            // Standing Progress Strategies
            builder.Services.AddScoped<IStandingProgressStrategy, GroupProgressStrategy>();
            builder.Services.AddScoped<IStandingProgressStrategy, BracketProgressStrategy>();

            // Start Groups Pipeline - Tournament group stage initialization
            builder.Services.AddScoped<IStartGroupsPipeline, StartGroupsPipeline>();
            // Pipeline Steps (order defined in StartGroupsPipeline.StepOrder)
            foreach (var step in StartGroupsPipeline.StepOrder)
                builder.Services.AddScoped(typeof(IStartGroupsPipelineStep), step);

            // Start Bracket Pipeline - Tournament bracket stage initialization
            builder.Services.AddScoped<IStartBracketPipeline, StartBracketPipeline>();
            // Pipeline Steps (order defined in StartBracketPipeline.StepOrder)
            foreach (var step in StartBracketPipeline.StepOrder)
                builder.Services.AddScoped(typeof(IStartBracketPipelineStep), step);
            // Bracket Seeding Strategies (one per format with a bracket)
            builder.Services.AddScoped<IBracketSeedingStrategy, RandomSeedingStrategy>();
            builder.Services.AddScoped<IBracketSeedingStrategy, GroupResultSeedingStrategy>();

            // Deps
            builder.Services.AddValidatorsFromAssemblyContaining<CreateTournamentValidator>();
            builder.Services.AddFluentValidationAutoValidation();

            // Exception Handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // API & Middleware
            builder.Services.AddControllers();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:4200" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Identity & JWT
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

            builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TO2DbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings?.Issuer,
                    ValidAudience = jwtSettings?.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"))),
                    // AuthService puts the username in the "name" claim; without this User.Identity.Name is null
                    NameClaimType = JwtRegisteredClaimNames.Name
                };

                // Configure SignalR authentication from query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TO2DbContext>();
                db.Database.Migrate();
            }

            app.UseExceptionHandler();
            app.UseCors("AllowFrontend");

            // Enable WebSockets for SignalR
            app.UseWebSockets();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<TournamentHub>("/hubs/tournament");

            app.MapControllers();

            app.Run();
        }
    }
}
