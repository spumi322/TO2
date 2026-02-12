import { FinalStanding } from './final-standing';

export interface GameResult {
  gameId: number;
  winnerId: number;
  teamAScore?: number;
  teamBScore?: number;
  matchId: number;
  standingId: number;
  tournamentId: number;
  rowVersion?: string | null;
}

/**
 * Response from backend when processing a game result.
 * Maps to backend GameProcessResultDTO.
 */
export interface GameProcessResult {
  success: boolean;
  matchFinished: boolean;
  matchWinnerId?: number;
  matchLoserId?: number;
  standingFinished?: boolean;
  allGroupsFinished?: boolean;
  tournamentFinished?: boolean;
  newTournamentStatus?: number;
  finalStandings?: FinalStanding[];
  message?: string;
}
