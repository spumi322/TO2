import { Team } from "./team";

export interface Tournament {
  id: number;
  name: string;
  description: string;
  maxTeams: number;
  format: Format;
  status: TournamentStatus;
  isRegistrationOpen: boolean;
  teamsPerGroup?: number;
  teamsPerBracket?: number;
  teams: Team[];
}

export enum Format {
  BracketOnly = 1,
  BracketAndGroups = 2,
}

export enum TournamentStatus {
  Setup = 1,
  SeedingGroups = 2,
  GroupsInProgress = 3,
  GroupsCompleted = 4,
  SeedingBracket = 5,
  BracketInProgress = 6,
  Finished = 7,
  Cancelled = 8
}

export interface TournamentStateDTO {
  currentStatus: TournamentStatus;
  isTransitionState: boolean;
  isActiveState: boolean;
  canScoreMatches: boolean;
  canModifyTeams: boolean;
  statusDisplayName: string;
  statusDescription: string;
}

export interface StartGroupsResponse {
  success: boolean;
  message: string;
  tournamentStatus: TournamentStatus;
}

export interface StartBracketResponse {
  success: boolean;
  message: string;
  tournamentStatus: TournamentStatus;
}
