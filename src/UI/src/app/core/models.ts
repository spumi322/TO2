import { Format, TeamStatus, TournamentStatus } from '../shared/enums';

// Response/request shapes of the backend DTOs (camelCase JSON)

export interface TournamentListItem {
  id: number;
  name: string;
  description: string | null;
  currentTeams: number;
  maxTeams: number;
  format: Format;
  status: TournamentStatus;
  version: number;
  // The only team placed 1st; null when not finished or several group winners (GroupsOnly)
  winnerName: string | null;
  // Finished group standings and bracket rounds (BYE matches count as played)
  groupsFinished: number;
  groupsTotal: number;
  roundsFinished: number;
  roundsTotal: number;
  finishedAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
}

export interface Tournament {
  id: number;
  name: string;
  description: string | null;
  maxTeams: number;
  format: Format;
  status: TournamentStatus;
  version: number;
  cancelledAt: string | null;
  cancelledBy: string | null;
  cancellationReason: string | null;
  currentTeams: number;
  numberOfGroups: number;
  advancingPerGroup: number | null;
  createdBy: string;
  createdDate: string;
}

export interface TournamentState {
  currentStatus: TournamentStatus;
  isTransitionState: boolean;
  isActiveState: boolean;
  canScoreMatches: boolean;
  canModifyTeams: boolean;
  canCancel: boolean;
  canDelete: boolean;
  canStartGroups: boolean;
  canStartBracket: boolean;
  version: number;
}

export interface CreateTournamentRequest {
  name: string;
  description: string | null;
  maxTeams: number;
  format: Format;
  numberOfGroups: number | null;
  advancingPerGroup: number | null;
}

export interface CreateTournamentResponse {
  id: number;
  version: number;
}

export interface Team {
  id: number;
  name: string;
}

export interface StandingGame {
  id: number;
  matchId: number;
  teamAId: number | null;
  teamAScore: number | null;
  teamBId: number | null;
  teamBScore: number | null;
  winnerId: number | null;
  version: number;
}

export interface StandingMatch {
  id: number;
  standingId: number;
  round: number | null;
  seed: number | null;
  teamAId: number | null;
  teamBId: number | null;
  winnerId: number | null;
  loserId: number | null;
  bestOf: number;
  version: number;
  teamAWins: number;
  teamBWins: number;
  games: StandingGame[];
}

export interface GroupTeam {
  id: number;
  name: string;
  wins: number;
  losses: number;
  points: number;
  gameWins: number;
  gameLosses: number;
  status: TeamStatus;
}

export interface Group {
  id: number;
  name: string;
  isFinished: boolean;
  isSeeded: boolean;
  version: number;
  teams: GroupTeam[];
  matches: StandingMatch[];
}

export interface Bracket {
  id: number;
  name: string;
  isFinished: boolean;
  isSeeded: boolean;
  version: number;
  matches: StandingMatch[];
}

export interface TeamPlacement {
  teamId: number;
  teamName: string;
  placement: number;
  status: TeamStatus;
  eliminatedInRound: number;
  groupName: string | null;
}

// Confirmed result of a whole match (version = the match version the page loaded)
export interface SetMatchResultRequest {
  matchId: number;
  standingId: number;
  tournamentId: number;
  teamAWins: number;
  teamBWins: number;
  version: number;
}

// A match's draft score: not saved; by = who is scoring it (null = this page)
export interface MatchDraft {
  teamAWins: number;
  teamBWins: number;
  by: string | null;
}

export interface GameResult {
  success: boolean;
  matchFinished: boolean;
  matchWinnerId: number | null;
  matchLoserId: number | null;
  standingFinished: boolean;
  allGroupsFinished: boolean;
  tournamentFinished: boolean;
  newTournamentStatus: TournamentStatus | null;
  finalStandings: TeamPlacement[] | null;
  message: string | null;
}

export interface StartStageResponse {
  success: boolean;
  message: string;
  tournamentStatus: TournamentStatus;
  version: number;
}
