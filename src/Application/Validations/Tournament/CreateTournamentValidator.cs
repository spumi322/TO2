using Application.DTOs.Tournament;
using Domain.Configuration;
using Domain.Enums;
using FluentValidation;

public class CreateTournamentValidator : AbstractValidator<CreateTournamentRequestDTO>
{
    private readonly ITournamentFormatConfiguration _formatConfig;

    public CreateTournamentValidator(ITournamentFormatConfiguration formatConfig)
    {
        _formatConfig = formatConfig;

        RuleFor(x => x.Name)
            .NotEmpty()
            .Length(4, 100)
            .WithMessage("Tournament name must be 4-100 characters long!");

        RuleFor(x => x.Description)
            .MaximumLength(250)
            .WithMessage("Description cannot exceed 250 characters.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid tournament format.");

        // Use configuration for MaxTeams validation
        RuleFor(x => x.MaxTeams)
            .Must((dto, maxTeams) =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);
                return maxTeams >= metadata.MinTeams && maxTeams <= metadata.MaxTeams;
            })
            .WithMessage(dto =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);
                return $"Max teams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
            });

        // TeamsPerBracket validation using configuration
        RuleFor(x => x.TeamsPerBracket)
            .Must((dto, teamsPerBracket) =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);

                // GroupsOnly: must be null
                if (!metadata.RequiresBracket)
                    return teamsPerBracket == null;
                // BracketAndGroups: must be valid range
                if (!teamsPerBracket.HasValue)
                    return false;

                return teamsPerBracket.Value >= metadata.MinTeamsPerBracket
                    && teamsPerBracket.Value <= metadata.MaxTeamsPerBracket;
            })
            .WithMessage(dto =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);
                
                if (!metadata.RequiresBracket)
                    return "Teams per bracket should not be set for GroupsOnly format.";

                return $"Teams per bracket must be between {metadata.MinTeamsPerBracket} and {metadata.MaxTeamsPerBracket}.";
            });

        // TeamsPerGroup validation - format specific
        RuleFor(x => x.TeamsPerGroup)
            .Must((dto, teamsPerGroup) =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);

                // BracketOnly: must be null
                if (!metadata.RequiresGroups)
                    return teamsPerGroup == null;

                // BracketAndGroups: must be in valid range
                if (!teamsPerGroup.HasValue)
                    return false;

                return teamsPerGroup.Value >= metadata.MinTeamsPerGroup
                    && teamsPerGroup.Value <= metadata.MaxTeamsPerGroup;
            })
            .WithMessage(dto =>
            {
                var metadata = _formatConfig.GetFormatMetadata(dto.Format);

                if (!metadata.RequiresGroups)
                    return "Teams per group should not be set for BracketOnly format.";

                return $"Teams per group must be between {metadata.MinTeamsPerGroup} and {metadata.MaxTeamsPerGroup}.";
            });

        // Use configuration for format-specific validation
        RuleFor(x => x)
            .Must(dto => _formatConfig.ValidateTeamConfiguration(
                dto.Format, dto.MaxTeams, dto.TeamsPerGroup, dto.TeamsPerBracket))
            .WithMessage(dto => _formatConfig.GetValidationErrorMessage(
                dto.Format, dto.MaxTeams, dto.TeamsPerGroup, dto.TeamsPerBracket));
    }
}
