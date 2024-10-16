import { Team } from "./team";

export interface Tournament {
  id: number;
  name: string;
  description: string;
  maxTeams: number;
  startDate: Date;
  endDate: Date;
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
  Upcoming = 1,
  Ongoing = 2,
  Finished = 3,
  Cancelled = 4,
}
