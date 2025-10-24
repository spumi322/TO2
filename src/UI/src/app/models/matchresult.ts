export interface MatchResult {
  teamAId: number;
  teamAWins: number;
  teamBId: number;
  teamBWins: number;
}

/**
 * Enhanced interface matching backend MatchResultDTO.
 * Includes tournament lifecycle state information.
 */
export interface MatchFinishedIds {
  winnerId: number;
  loserId: number;
  allGroupsFinished?: boolean;
  tournamentFinished?: boolean;
}
