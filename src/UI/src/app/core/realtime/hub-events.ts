import { TournamentStatus } from '../../shared/enums';
import { Bracket, Group, StandingGame, StandingMatch, TeamPlacement } from '../models';

// Payloads of the events SignalRService (backend) sends

export interface TournamentEvent {
  tournamentId: number;
  updatedBy: string;
}

export interface TeamAddedEvent extends TournamentEvent {
  teamIds: number[];
}

export interface TeamRemovedEvent extends TournamentEvent {
  teamId: number;
}

export interface GameUpdatedEvent extends TournamentEvent {
  gameId: number;
  matchId: number;
  standingId: number;
  game: StandingGame | null;
  match: StandingMatch | null;
  matchFinished: boolean;
  standingFinished: boolean;
  allGroupsFinished: boolean;
  tournamentFinished: boolean;
  // The whole changed standing: exactly one of the two is set
  updatedGroup: Group | null;
  updatedBracket: Bracket | null;
  finalStandings: TeamPlacement[] | null;
  newTournamentStatus: TournamentStatus | null;
}

// A draft score someone else is entering (relayed by the hub, never saved)
export interface MatchDraftUpdatedEvent extends TournamentEvent {
  matchId: number;
  teamAWins: number;
  teamBWins: number;
}

export interface HubEventMap {
  // tenant group
  TournamentCreated: TournamentEvent;
  TournamentUpdated: TournamentEvent;
  TournamentDeleted: TournamentEvent;
  TournamentCancelled: TournamentEvent;
  TeamAdded: TeamAddedEvent;
  TeamRemoved: TeamRemovedEvent;
  // tournament group (after JoinTournament)
  GameUpdated: GameUpdatedEvent;
  GroupsStarted: TournamentEvent;
  BracketStarted: TournamentEvent;
  MatchDraftUpdated: MatchDraftUpdatedEvent;
}

export type HubEventName = keyof HubEventMap;

export const HUB_EVENT_NAMES: HubEventName[] = [
  'TournamentCreated',
  'TournamentUpdated',
  'TournamentDeleted',
  'TournamentCancelled',
  'TeamAdded',
  'TeamRemoved',
  'GameUpdated',
  'GroupsStarted',
  'BracketStarted',
  'MatchDraftUpdated',
];

export const TENANT_EVENT_NAMES: HubEventName[] = HUB_EVENT_NAMES.slice(0, 6);
