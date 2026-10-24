using Application.Contracts;
using Application.DTOs.Tournament;
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
            .Length(4, 60)
            .WithMessage("Tournament name must be 4-60 characters long.");

        RuleFor(x => x.Description)
            .MaximumLength(120)
            .WithMessage("Description cannot exceed 120 characters.");

        RuleFor(x => x.Format)
            .IsInEnum()
            .WithMessage("Invalid tournament format.");

        // BRACKET ONLY FORMAT

        When(x => x.Format == Format.BracketOnly, () =>
        {
            // 1. NumberOfGroups must be null
            RuleFor(x => x)
                .Must(dto => dto.NumberOfGroups == null)
                .WithMessage("The Bracket format has no groups.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 2. AdvancingPerGroup must be null
            RuleFor(x => x)
                .Must(dto => dto.AdvancingPerGroup == null)
                .WithMessage("The Bracket format has no group stage to advance from.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));

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
                    return $"Max teams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));
        });

        // GROUPS + BRACKET FORMAT

        When(x => x.Format == Format.GroupsAndBracket, () =>
        {
            // 1. NumberOfGroups required
            RuleFor(x => x)
                .Must(dto => dto.NumberOfGroups.HasValue)
                .WithMessage("Set the number of groups.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 2. AdvancingPerGroup required
            RuleFor(x => x)
                .Must(dto => dto.AdvancingPerGroup.HasValue)
                .WithMessage("Set how many teams advance per group.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));

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
                    return $"Max teams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 4. NumberOfGroups >= 1
            RuleFor(x => x)
                .Must(dto => !dto.NumberOfGroups.HasValue || dto.NumberOfGroups.Value >= 1)
                .WithMessage("At least 1 group.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 5. Minimum group size >= 3
            RuleFor(x => x)
                .Must(dto => !dto.NumberOfGroups.HasValue || dto.NumberOfGroups.Value == 0 ||
                             dto.MaxTeams / dto.NumberOfGroups.Value >= 3)
                .WithMessage("Each group needs at least 3 teams.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 6. AdvancingPerGroup >= 1
            RuleFor(x => x)
                .Must(dto => !dto.AdvancingPerGroup.HasValue || dto.AdvancingPerGroup.Value >= 1)
                .WithMessage("At least 1 team must advance.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));

            // 7. When NumberOfGroups == 1, AdvancingPerGroup must be >= 2 (bracket needs at least 2 teams)
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.AdvancingPerGroup.HasValue || !dto.NumberOfGroups.HasValue) return true;
                    if (dto.NumberOfGroups.Value != 1) return true;
                    return dto.AdvancingPerGroup.Value >= 2;
                })
                .WithMessage("With one group, at least 2 teams must advance.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));

            // 8. AdvancingPerGroup < floor(MaxTeams / NumberOfGroups) — at least 1 eliminated per group
            RuleFor(x => x)
                .Must(dto =>
                {
                    if (!dto.AdvancingPerGroup.HasValue || !dto.NumberOfGroups.HasValue) return true;
                    if (dto.NumberOfGroups.Value == 0) return true;
                    int minGroupSize = dto.MaxTeams / dto.NumberOfGroups.Value;
                    return dto.AdvancingPerGroup.Value < minGroupSize;
                })
                .WithMessage("At least one team per group must be eliminated.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));
        });

        // GROUPS ONLY FORMAT

        When(x => x.Format == Format.GroupsOnly, () =>
        {
            // 1. NumberOfGroups required
            RuleFor(x => x)
                .Must(dto => dto.NumberOfGroups.HasValue)
                .WithMessage("Set the number of groups.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 2. AdvancingPerGroup must be null
            RuleFor(x => x)
                .Must(dto => dto.AdvancingPerGroup == null)
                .WithMessage("The Groups format has no advancing teams.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.AdvancingPerGroup));

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
                    return $"Max teams must be between {metadata.MinTeams} and {metadata.MaxTeams}.";
                })
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.MaxTeams));

            // 4. NumberOfGroups >= 1
            RuleFor(x => x)
                .Must(dto => !dto.NumberOfGroups.HasValue || dto.NumberOfGroups.Value >= 1)
                .WithMessage("At least 1 group.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));

            // 5. Minimum group size >= 3
            RuleFor(x => x)
                .Must(dto => !dto.NumberOfGroups.HasValue || dto.NumberOfGroups.Value == 0 ||
                             dto.MaxTeams / dto.NumberOfGroups.Value >= 3)
                .WithMessage("Each group needs at least 3 teams.")
                .OverridePropertyName(nameof(CreateTournamentRequestDTO.NumberOfGroups));
        });
    }
}
