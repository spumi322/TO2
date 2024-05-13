import { Team } from "./team";

export interface Game {
  id: number;
  matchId: number;
  winner: Team;
  teamAScore: number;
  teamBScore: number;
  duration: number;
}
