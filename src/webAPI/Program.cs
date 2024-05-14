using Application.Common;
using Application.Contracts;
using Application.Services;
using Domain.AggregateRoots;
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

            builder.Services.AddScoped<ITournamentService, TournamentService>();
            builder.Services.AddScoped<IStandingService, StandingService>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            builder.Services.AddDbContext<TO2DbContext>(options =>
            {
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddAutoMapper(typeof(MappingProfile));
            builder.Services.AddFluentValidation().AddValidatorsFromAssemblyContaining<IAssemblyMarker>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
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
