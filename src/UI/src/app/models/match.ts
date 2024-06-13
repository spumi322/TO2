import { Game } from "./game";
import { MatchResult } from "./matchresult";

export interface Match {
  id: number;
  standingId: number;
  round: number;
  seed: number;
  teamAId: number;
  teamBId: number;
  winnerId: number;
  loserId: number;
  bestOf: BestOf;
  games: Game[];
  result: MatchResult;
}

export enum BestOf {
  Bo1 = 1,
  Bo2 = 2,
  Bo3 = 3,
  Bo5 = 5,
}
