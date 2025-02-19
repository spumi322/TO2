using Application.DTOs.Tournament;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validations.Tournament
{
    public class UpdateTournamentValidator : AbstractValidator<UpdateTournamentRequestDTO>
    {
        public UpdateTournamentValidator()
        {
            RuleFor(x => x.Name)                
                .NotEmpty()
                .Length(4, 100)
                .WithMessage("Tournament name must be 4-100 character long!");

            RuleFor(x => x.Description)
                .MaximumLength(250);
        }
    }
}
