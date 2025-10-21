-- GroupPlayFlowView: Comprehensive view of tournament flow data
-- Shows relationships between tournaments, standings, matches, games, and group entries
-- Note: This is a denormalized view - rows will duplicate when there are multiple related records

-- CREATE VIEW IF NOT EXISTS GroupPlayFlowView AS
SELECT
    -- Tournament fields
    t.Id AS TournamentId,
    t.Name AS TournamentName,
    t.Status AS TournamentStatus,
    t.IsRegistrationOpen AS TournamentIsRegistrationOpen,

    -- Standing fields
    s.Id AS StandingId,
    s.Name AS StandingName,
    s.MaxTeams AS StandingMaxTeams,
    s.StandingType AS StandingType,
    s.IsFinished AS StandingIsFinished,
    s.IsSeeded AS StandingIsSeeded,

    -- Match fields
    m.Id AS MatchId,
    m.Round AS MatchRound,
    m.Seed AS MatchSeed,
    m.TeamAId AS MatchTeamAId,
    m.TeamBId AS MatchTeamBId,
    m.WinnerId AS MatchWinnerId,
    m.LoserId AS MatchLoserId,

    -- Game fields
    g.Id AS GameId,
    g.WinnerId AS GameWinnerId,
    g.TeamAId AS GameTeamAId,
    g.TeamBId AS GameTeamBId,

    -- Group Entry fields
    ge.TeamId AS GroupTeamId,
    ge.TeamName AS GroupTeamName,
    ge.Status AS GroupStatus,
    ge.Eliminated AS GroupEliminated,
    ge.Wins AS GroupWins,
    ge.Losses AS GroupLosses,
    ge.Points AS GroupPoints

FROM Tournaments t

LEFT JOIN Standings s
    ON s.TournamentId = t.Id

LEFT JOIN Matches m
    ON m.StandingId = s.Id

LEFT JOIN Games g
    ON g.MatchId = m.Id

LEFT JOIN GroupEntries ge
    ON ge.TournamentId = t.Id
    AND ge.StandingId = s.Id;
