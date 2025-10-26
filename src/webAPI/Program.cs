using Application.Common;
using Application.Contracts;
using Application.Pipelines.GameResult;
using Application.Pipelines.GameResult.Contracts;
using Application.Pipelines.GameResult.Steps;
using Application.Pipelines.GameResult.Strategies;
using Application.Services;
using Domain.StateMachine;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repository;
using Infrastructure.Profiles;
using Microsoft.EntityFrameworkCore;

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
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            // UoW, Repos
            builder.Services.AddScoped<ITO2DbContext, TO2DbContext>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Domain Services
            builder.Services.AddScoped<ITournamentStateMachine, TournamentStateMachine>();
            // Application Services
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IGameService, GameService>();
            builder.Services.AddScoped<Func<IGameService>>(sp => () => sp.GetRequiredService<IGameService>());
            // STEP 2: Lifecycle Service - Replaces domain event handlers
            builder.Services.AddScoped<IOrchestrationService, OrchestrationService>();
            builder.Services.AddScoped<IWorkFlowService, WorkFlowService>();
            builder.Services.AddScoped<Func<IOrchestrationService>>(sp => () => sp.GetRequiredService<IOrchestrationService>());

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

            // Deps
            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddFluentValidation().AddValidatorsFromAssemblyContaining<IAssemblyMarker>();
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
