using Application.DTOs.Tournament;
using FluentValidation;

namespace Application.Validations.Tournament
{
    public class CancelTournamentValidator : AbstractValidator<CancelTournamentRequestDTO>
    {
        public CancelTournamentValidator()
        {
            RuleFor(x => x.Reason)
                .MaximumLength(250)
                .WithMessage("Cancellation reason must be at most 250 characters.");
        }
    }
}
