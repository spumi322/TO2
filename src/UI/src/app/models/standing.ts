import { Match } from "./match";

export interface Standing {
  id: number;
  tournamentId: number;
  name: string;
  type: StandingType;
  startDate: Date;
  endDate: Date;
  maxTeams: number;
  numberOfTeams: number;
  matches: Match[];
}

export enum StandingType {
  Group = 1,
  Bracket = 2,
}
