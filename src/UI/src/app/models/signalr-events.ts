import { TournamentStatus } from './tournament';
import { Standing } from './standing';
import { Game } from './game';
import { Match } from './match';
import { FinalStanding } from './final-standing';

export interface GameUpdatedEvent {
  tournamentId: number;
  gameId: number;
  matchId: number;
  standingId: number;
  updatedBy: string;
  game: Game;
  match: Match;
  matchFinished: boolean;
  standingFinished: boolean;
  allGroupsFinished: boolean;
  tournamentFinished: boolean;
  updatedGroup?: Standing;
  updatedBracket?: Standing;
  finalStandings?: FinalStanding[];
  newTournamentStatus?: TournamentStatus;
}
