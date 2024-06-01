import { Match } from "./match";
import { Team } from "./team";

export interface Standing {
  id: number;
  tournamentId: number;
  name: string;
  type: StandingType;
  startDate: Date;
  endDate: Date;
  maxTeams: number;
  matches: Match[];
  teams: Team[];
}

export enum StandingType {
  Group = 1,
  Bracket = 2,
}
