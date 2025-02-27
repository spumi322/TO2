using Application.Common;
using Application.Contracts;
using Application.Services;
using Application.Services.EventHandlers;
using Application.Services.EventHandling;
using Domain.AggregateRoots;
using Domain.DomainEvents;
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
            // Services
            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<IMatchService, MatchService>();
            builder.Services.AddScoped<IGameService, GameService>();
            // Handlers
            builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
            builder.Services.AddScoped<IDomainEventHandler<StandingFinishedEvent>, StandingFinishedEventHandler>();
            builder.Services.AddScoped<IDomainEventHandler<AllGroupsFinishedEvent>, AllGroupsFinishedEventHandler>();
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
