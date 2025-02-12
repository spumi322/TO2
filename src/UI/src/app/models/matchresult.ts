export interface MatchResult {
  teamAId: number;
  teamAWins: number;
  teamBId: number;
  teamBWins: number;
}

export interface MatchFinishedIds {
  id: number;
  winnerId: number;
  loserId: number;
}
