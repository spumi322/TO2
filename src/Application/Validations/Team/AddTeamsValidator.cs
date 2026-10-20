using Application.DTOs.Team;
using FluentValidation;

namespace Application.Validations.Team
{
    public class AddTeamsValidator : AbstractValidator<AddTeamsRequestDTO>
    {
        public const int MaxNames = 32;
        public const int MaxNameLength = 24;

        public AddTeamsValidator()
        {
            RuleFor(x => x.Names)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .WithMessage("Add at least one team name!")
                .Must(names => names.Count <= MaxNames)
                .WithMessage($"At most {MaxNames} teams can be added at once!")
                .Must(names => names.Select(n => n?.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count)
                .WithMessage("Team names must be unique!");

            RuleForEach(x => x.Names)
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Team name cannot be empty!")
                .Must(name => name == null || name.Trim().Length <= MaxNameLength)
                .WithMessage($"Team name cannot exceed {MaxNameLength} characters!");
        }
    }
}
