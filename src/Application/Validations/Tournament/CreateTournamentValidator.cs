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

        // Outcommented for testing purposes

        //RuleFor(x => x.StartDate)
        //    .NotEmpty()
        //    .GreaterThan(DateTime.UtcNow)
        //    .WithMessage("Start date must be in the future.");

        //RuleFor(x => x.EndDate)
        //    .NotEmpty()
        //    .GreaterThan(x => x.StartDate)
        //    .WithMessage("End date must be after the start date.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid tournament format.");

        RuleFor(x => x.TeamsPerBracket)
            .NotEmpty()
            .InclusiveBetween(4, 32)
            .WithMessage("Teams per bracket must be between 4 and 32.");

        RuleFor(x => x.TeamsPerGroup)
            .NotEmpty()
            .InclusiveBetween(2, 16)
            .When(x => x.Format == Format.BracketAndGroup)
            .WithMessage("For BracketAndGroup format, teams per group must be between 2 and 16.");

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
            .When(x => x.Format == Format.BracketAndGroup)
            .WithMessage("For BracketAndGroup format, MaxTeams must be divisible by TeamsPerGroup.");
    }
}