using Application.DTOs.Tournament;
using Domain.Enums;
using FluentValidation;

public class CreateTournamentValidator : AbstractValidator<CreateTournamentRequestDTO>
{
    public CreateTournamentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(4, 100)
            .WithMessage("Tournament name must be 4-100 characters long!");

        RuleFor(x => x.Description)
            .MaximumLength(250)
            .WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.MaxTeams)
             .InclusiveBetween(2, 32)
             .WithMessage("Max teams must be between 2 and 32.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid tournament format.");

        RuleFor(x => x.TeamsPerBracket)
            .NotEmpty()
            .InclusiveBetween(4, 32)
            .WithMessage("Teams per bracket must be between 4 and 32.");

        RuleFor(x => x.TeamsPerGroup)
            .NotNull().WithMessage("TeamsPerGroup is required")
            .GreaterThan(0).WithMessage("TeamsPerGroup must be greater than 0")
            .InclusiveBetween(2, 16).WithMessage("Teams per group must be between 2 and 16")
            .When(x => x.Format == Format.BracketAndGroup);

        RuleFor(x => x.TeamsPerGroup)
            .Null()
            .When(x => x.Format == Format.BracketOnly)
            .WithMessage("TeamsPerGroup should not be set for BracketOnly format.");

        RuleFor(x => x)
            .Must(x => x.MaxTeams == x.TeamsPerBracket)
            .When(x => x.Format == Format.BracketOnly)
            .WithMessage("For BracketOnly format, MaxTeams must equal TeamsPerBracket.");

        RuleFor(x => x)
            .Must(x => x.MaxTeams % (x.TeamsPerGroup ?? 1) == 0)
            .When(x => x.Format == Format.BracketAndGroup && x.TeamsPerGroup.HasValue && x.TeamsPerGroup.Value > 0)
            .WithMessage("For BracketAndGroup format, MaxTeams must be divisible by TeamsPerGroup.");
    }
}