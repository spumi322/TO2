using Application.DTOs.Standing;
using Application.DTOs.Tournament;
using Domain.Enums;

namespace Application.DTOs.SignalR;

public record GameUpdatedEvent(
    long TournamentId,
    long GameId,
    long MatchId,
    long StandingId,
    string UpdatedBy,
    StandingGameDTO? Game,
    StandingMatchDTO? Match,
    bool MatchFinished,
    bool StandingFinished,
    bool AllGroupsFinished,
    bool TournamentFinished,
    GetGroupsWithDetailsResponseDTO? UpdatedGroup,
    GetBracketWithDetailsResponseDTO? UpdatedBracket,
    List<TeamPlacementDTO>? FinalStandings,
    TournamentStatus? NewTournamentStatus
);
