using Application.DTOs.Team;
using FluentValidation;

namespace Application.Validations.Team
{
    public class CreateTeamValidator : AbstractValidator<CreateTeamRequestDTO>
    {
        public CreateTeamValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(24)
                .WithMessage("Team name cannot exceed 24 characters.");
        }
    }
}
