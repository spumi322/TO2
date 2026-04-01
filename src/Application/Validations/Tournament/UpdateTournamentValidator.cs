using Application.DTOs.Tournament;
using FluentValidation;

namespace Application.Validations.Tournament
{
    public class UpdateTournamentValidator : AbstractValidator<UpdateTournamentRequestDTO>
    {
        public UpdateTournamentValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .Length(4, 60)
                .WithMessage("Tournament name must be 4-60 characters long!");

            RuleFor(x => x.Description)
                .MaximumLength(250);
        }
    }
}
