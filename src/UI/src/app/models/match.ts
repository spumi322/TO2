import { Game } from "./game";
import { Team } from "./team";

export interface Match {
  id: number;
  standingId: number;
  teamA: Team;
  teamB: Team;
  bestOf: BestOf;
  games: Game[];
}

export enum BestOf {
  Bo1 = 1,
  Bo2 = 2,
  Bo3 = 3,
  Bo5 = 5,
}
