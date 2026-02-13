using Application.Configurations;
using Application.Validations.Tournament;
using Application.Contracts;
using Application.Contracts.Repositories;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Application.Pipelines.GameResult.Strategies;
using Application.Pipelines.StartBracket;
using Application.Pipelines.StartBracket.Contracts;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using Application.Services;
using Domain.Entities;
using Domain.StateMachine;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Repository;
using Infrastructure.Profiles;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
                           serviceProvider.GetRequiredService<AuditInterceptor>()
                       );
            });

            // UoW, Repos
            builder.Services.AddScoped<ITO2DbContext, TO2DbContext>();
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

            // EF Core Interceptors (Modern approach for tenant isolation and auditing)
            builder.Services.AddScoped<TenantSaveChangesInterceptor>();
            builder.Services.AddScoped<AuditInterceptor>();

            // Application Services
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<IAuthService, AuthService>();

            // STEP 2: Lifecycle Service - Replaces domain event handlers
            builder.Services.AddScoped<IWorkFlowService, WorkFlowService>();
            // Game Result Pipeline - SOLID refactoring of ProcessGameResult
            builder.Services.AddScoped<IGameResultPipeline, GameResultPipeline>();
            // Pipeline Steps (registered in execution order)
            builder.Services.AddScoped<IGameResultPipelineStep, ScoreGameStep>();
            builder.Services.AddScoped<IGameResultPipelineStep, CheckMatchCompletionStep>();
            builder.Services.AddScoped<IGameResultPipelineStep, UpdateGroupStatsStep>();
            builder.Services.AddScoped<IGameResultPipelineStep, HandleStandingProgressStep>();
            builder.Services.AddScoped<IGameResultPipelineStep, TransitionTournamentStateStep>();
            builder.Services.AddScoped<IGameResultPipelineStep, CalculateFinalPlacementsStep>();
            // BroadcastUpdatesStep removed - broadcasting now happens after transaction commit in pipeline executor
            builder.Services.AddScoped<IGameResultPipelineStep, BuildResponseStep>();
            // Standing Progress Strategies
            builder.Services.AddScoped<IStandingProgressStrategy, GroupProgressStrategy>();
            builder.Services.AddScoped<IStandingProgressStrategy, BracketProgressStrategy>();

            // Start Groups Pipeline - Tournament group stage initialization
            builder.Services.AddScoped<IStartGroupsPipeline, StartGroupsPipeline>();
            // Pipeline Steps (registered in execution order)
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.ValidateAndTransitionToSeedingGroupsStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.ValidateStandingsNotSeededStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.DistributeTeamsIntoGroupsStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.CreateGroupEntriesStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.GenerateRoundRobinMatchesStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.MarkStandingsAsSeededStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.TransitionToGroupsInProgressStep>();
            builder.Services.AddScoped<IStartGroupsPipelineStep, Application.Pipelines.StartGroups.Steps.BuildResponseStep>();

            // Start Bracket Pipeline - Tournament bracket stage initialization
            builder.Services.AddScoped<IStartBracketPipeline, StartBracketPipeline>();
            // Pipeline Steps (registered in execution order)
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.ValidateAndTransitionToSeedingBracketStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.ValidateBracketNotSeededStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.GetAdvancedTeamsStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.RandomSeedTeamsStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.ValidateTeamCountStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.CalculateBracketStructureStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.GenerateBracketMatchesStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.MarkBracketAsSeededStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.TransitionToBracketInProgressStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.BuildResponseStep>();

            // Deps
            builder.Services.AddAutoMapper(typeof(MappingProfile));
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
                        Encoding.UTF8.GetBytes(jwtSettings?.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured")))
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
