using Application.Contracts;
using Application.DTOs.Tournament;
using Application.Pipelines.StartBracket.Utilities;
using Domain.Enums;
using FluentValidation;

public class CreateTournamentValidator : AbstractValidator<CreateTournamentRequestDTO>
{
    private readonly IFormatService _formatService;

    public CreateTournamentValidator(IFormatService formatService)
    {
        _formatService = formatService;

        // GENERAL VALIDATION (applies to ALL formats)

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

        // BRACKET ONLY FORMAT

        When(x => x.Format == Format.BracketOnly, () =>
        {
            // 1. TeamsPerGroup must be null
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerGroup == null)
                .WithMessage("TeamsPerGroup should not be set for BracketOnly format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 2. TeamsPerBracket required
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerBracket.HasValue)
                .WithMessage("TeamsPerBracket is required for BracketOnly format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 3. MaxTeams range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.MaxTeams >= metadata.MinTeams && dto.MaxTeams <= metadata.MaxTeams;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"MaxTeams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 4. MaxTeams power-of-2 check
            RuleFor(x => x)
                .Must(dto => BracketSeedingUtility.IsPowerOfTwo(dto.MaxTeams))
                .WithMessage("For BracketOnly format, MaxTeams must be a power of 2 (4, 8, 16, 32).")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 5. TeamsPerBracket range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.TeamsPerBracket.HasValue) return true;
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.TeamsPerBracket.Value >= metadata.MinTeamsPerBracket
                        && dto.TeamsPerBracket.Value <= metadata.MaxTeamsPerBracket;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"TeamsPerBracket must be between {metadata.MinTeamsPerBracket} and {metadata.MaxTeamsPerBracket}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 6. MaxTeams must equal TeamsPerBracket
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerBracket.HasValue || dto.MaxTeams == dto.TeamsPerBracket.Value)
                .WithMessage("For BracketOnly format, MaxTeams must equal TeamsPerBracket.");
        });

        // GROUPS + BRACKET FORMAT

        When(x => x.Format == Format.GroupsAndBracket, () =>
        {
            // 1. TeamsPerGroup required
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerGroup.HasValue)
                .WithMessage("TeamsPerGroup is required for GroupsAndBracket format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 2. TeamsPerBracket required
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerBracket.HasValue)
                .WithMessage("TeamsPerBracket is required for GroupsAndBracket format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 3. MaxTeams range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.MaxTeams >= metadata.MinTeams && dto.MaxTeams <= metadata.MaxTeams;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"MaxTeams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 4. TeamsPerGroup range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.TeamsPerGroup.HasValue) return true;
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.TeamsPerGroup.Value >= metadata.MinTeamsPerGroup
                        && dto.TeamsPerGroup.Value <= metadata.MaxTeamsPerGroup;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"TeamsPerGroup must be between {metadata.MinTeamsPerGroup} and {metadata.MaxTeamsPerGroup}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 5. TeamsPerBracket range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.TeamsPerBracket.HasValue) return true;
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.TeamsPerBracket.Value >= metadata.MinTeamsPerBracket
                        && dto.TeamsPerBracket.Value <= metadata.MaxTeamsPerBracket;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"TeamsPerBracket must be between {metadata.MinTeamsPerBracket} and {metadata.MaxTeamsPerBracket}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 6. TeamsPerGroup <= MaxTeams
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerGroup.HasValue || dto.TeamsPerGroup.Value <= dto.MaxTeams)
                .WithMessage("TeamsPerGroup cannot be greater than MaxTeams.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 7. TeamsPerBracket <= MaxTeams
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerBracket.HasValue || dto.TeamsPerBracket.Value <= dto.MaxTeams)
                .WithMessage("TeamsPerBracket cannot be greater than MaxTeams.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 8. TeamsPerBracket power-of-2 check
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerBracket.HasValue || BracketSeedingUtility.IsPowerOfTwo(dto.TeamsPerBracket.Value))
                .WithMessage("For GroupsAndBracket format, TeamsPerBracket must be a power of 2 (4, 8, 16, 32).")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 9. MaxTeams divisible by TeamsPerGroup
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerGroup.HasValue || dto.TeamsPerGroup.Value == 0 || dto.MaxTeams % dto.TeamsPerGroup.Value == 0)
                .WithMessage("MaxTeams must be divisible by TeamsPerGroup.");

            // 10. TeamsPerBracket divisible by number of groups
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.TeamsPerBracket.HasValue || !dto.TeamsPerGroup.HasValue) return true;
                    if (dto.TeamsPerGroup.Value == 0) return true;
                    int numberOfGroups = dto.MaxTeams / dto.TeamsPerGroup.Value;
                    if (numberOfGroups == 0) return true;
                    return dto.TeamsPerBracket.Value % numberOfGroups == 0;
                })
                .WithMessage("TeamsPerBracket must be divisible by the number of groups (MaxTeams / TeamsPerGroup).");
        });

        // GROUPS ONLY FORMAT

        When(x => x.Format == Format.GroupsOnly, () =>
        {
            // 1. TeamsPerGroup required
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerGroup.HasValue)
                .WithMessage("TeamsPerGroup is required for GroupsOnly format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 2. TeamsPerBracket must be null
            RuleFor(x => x)
                .Must(dto => dto.TeamsPerBracket == null)
                .WithMessage("TeamsPerBracket should not be set for GroupsOnly format.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerBracket));

            // 3. MaxTeams range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.MaxTeams >= metadata.MinTeams && dto.MaxTeams <= metadata.MaxTeams;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"MaxTeams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 4. TeamsPerGroup range check
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.TeamsPerGroup.HasValue) return true;
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return dto.TeamsPerGroup.Value >= metadata.MinTeamsPerGroup
                        && dto.TeamsPerGroup.Value <= metadata.MaxTeamsPerGroup;
                })
                .WithMessage(dto =>
                {
                    var metadata = _formatService.GetFormatMetadata(dto.Format);
                    return $"TeamsPerGroup must be between {metadata.MinTeamsPerGroup} and {metadata.MaxTeamsPerGroup}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.TeamsPerGroup));

            // 5. MaxTeams divisible by TeamsPerGroup
            RuleFor(x => x)
                .Must(dto => !dto.TeamsPerGroup.HasValue || dto.TeamsPerGroup.Value == 0 || dto.MaxTeams % dto.TeamsPerGroup.Value == 0)
                .WithMessage("MaxTeams must be divisible by TeamsPerGroup.");
        });
    }
}
