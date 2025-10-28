using Application.Common;
using Application.Contracts;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Application.Pipelines.GameResult.Strategies;
using Application.Pipelines.StartGroups;
using Application.Pipelines.StartGroups.Contracts;
using Application.Pipelines.StartBracket;
using Application.Pipelines.StartBracket.Contracts;
using Application.Services;
using Domain.StateMachine;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;
using Application.Contracts.Repositories;
using webAPI.Middleware;

namespace TO2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // DbContext
            builder.Services.AddDbContext<TO2DbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            // UoW, Repos
            builder.Services.AddScoped<ITO2DbContext, TO2DbContext>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ITournamentRepository, TournamentRepository>();
            builder.Services.AddScoped<IGroupRepository, GroupRepository>();
            builder.Services.AddScoped<ITournamentTeamRepository, TournamentTeamRepository>();

            // Domain Services
            builder.Services.AddScoped<ITournamentStateMachine, TournamentStateMachine>();
            // Application Services
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IGameService, GameService>();
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
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.ValidateTeamCountStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.CalculateBracketStructureStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.GenerateBracketMatchesStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.MarkBracketAsSeededStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.TransitionToBracketInProgressStep>();
            builder.Services.AddScoped<IStartBracketPipelineStep, Application.Pipelines.StartBracket.Steps.BuildResponseStep>();

            // Deps
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddFluentValidation().AddValidatorsFromAssemblyContaining<IAssemblyMarker>();
            // Exception Handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            // API & Middleware
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("https://127.0.0.1:4200", "https://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            app.UseExceptionHandler();
            app.UseCors();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(s =>
                {
                    s.SwaggerEndpoint("/swagger/v1/swagger.json", "TO2 API");
                    s.RoutePrefix = string.Empty;
                });
            }

            app.MapControllers();

            app.Run();
        }
    }
}
