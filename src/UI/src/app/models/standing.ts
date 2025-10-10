import { Match } from "./match";
import { Team } from "./team";

export interface Standing {
  id: number;
  tournamentId: number;
  name: string;
  standingType: StandingType;
  maxTeams: number;
  matches: Match[];
  teams: Team[];
  isFinished: boolean;
  isSeeded: boolean;
}

export enum StandingType {
  Group = 1,
  Bracket = 2,
}
