using Application.DTOs.Tournament;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations.Tournament
{
    public class CreateTournamentValidator : AbstractValidator<CreateTournamentRequestDTO>
    {
        public CreateTournamentValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .Length(4, 100)
                .WithMessage("Tournament name must be 4-100 character long!");

            RuleFor(x => x.Description)
                .MaximumLength(250);

            RuleFor(x => x.MaxTeams)
                 .InclusiveBetween(2, 32)
                 .WithMessage("Max teams must be between 2 and 32.");

// Outcommented for development purposes

            //RuleFor(x => x.StartDate)
            //    .NotEmpty()
            //    .GreaterThan(DateTime.UtcNow)
            //    .WithMessage("Start date must be in the future.");

            //RuleFor(x => x.EndDate)
            //    .NotEmpty()
            //    .GreaterThan(x => x.StartDate)
            //    .WithMessage("End date must be after the start date.");

            RuleFor(x => x.Format)
                .IsInEnum();
        }
    }
}
