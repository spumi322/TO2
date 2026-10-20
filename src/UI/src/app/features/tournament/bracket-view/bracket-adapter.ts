import type { Match as ViewerMatch, MatchGame as ViewerGame, Participant, ParticipantResult, Stage } from 'brackets-model';
import { Bracket, StandingMatch } from '../../../core/models';

// brackets-model's Status enum, as plain numbers (the package is only used for types)
const STATUS = { locked: 0, waiting: 1, ready: 2, completed: 4 } as const;

export interface ViewerData {
  stages: Stage[];
  matches: ViewerMatch[];
  matchGames: ViewerGame[];
  participants: Participant[];
}

/**
 * Our bracket DTO -> brackets-viewer data (pure, no library code).
 * Slot rules of the library: `null` opponent = BYE, `{ id: null }` = not known yet (TBD).
 * Ours: an empty slot in round 1 is a BYE (that match already has its winner); in later rounds it is TBD.
 */
export function toViewerData(bracket: Bracket, teamNames: Map<number, string>, tournamentId: number): ViewerData {
  const stage: Stage = {
    id: bracket.id,
    tournament_id: tournamentId,
    name: bracket.name,
    type: 'single_elimination',
    number: 1,
    settings: {},
  };

  const matches = bracket.matches.map((match) => toViewerMatch(match, bracket.id));

  const matchGames = bracket.matches.flatMap((match) =>
    [...match.games]
      .sort((a, b) => a.id - b.id)
      .map(
        (game, index): ViewerGame => ({
          id: game.id,
          stage_id: stage.id,
          parent_id: match.id,
          number: index + 1,
          status: game.winnerId ? STATUS.completed : STATUS.ready,
          opponent1: { id: game.teamAId, result: resultOf(game.winnerId, game.teamAId) },
          opponent2: { id: game.teamBId, result: resultOf(game.winnerId, game.teamBId) },
        }),
      ),
  );

  const participantIds = new Set(
    bracket.matches.flatMap((match) => [match.teamAId, match.teamBId]).filter((id): id is number => !!id),
  );
  const participants = [...participantIds].map(
    (id): Participant => ({ id, tournament_id: tournamentId, name: teamNames.get(id) ?? 'Unknown team' }),
  );

  return { stages: [stage], matches, matchGames, participants };
}

function toViewerMatch(match: StandingMatch, stageId: number): ViewerMatch {
  const round = match.round ?? 1;
  const isBye = round === 1 && (!match.teamAId || !match.teamBId);

  return {
    id: match.id,
    stage_id: stageId,
    group_id: 1,
    round_id: round,
    number: match.seed ?? 1,
    child_count: isBye ? 0 : match.bestOf,
    status: statusOf(match),
    opponent1: slot(match.teamAId, match.teamAWins, match.winnerId, isBye),
    opponent2: slot(match.teamBId, match.teamBWins, match.winnerId, isBye),
  };
}

function slot(teamId: number | null, wins: number, winnerId: number | null, isBye: boolean): ParticipantResult | null {
  if (!teamId) return isBye ? null : { id: null };
  return { id: teamId, score: isBye ? undefined : wins, result: resultOf(winnerId, teamId) };
}

function resultOf(winnerId: number | null, teamId: number | null): 'win' | 'loss' | undefined {
  if (!winnerId || !teamId) return undefined;
  return winnerId === teamId ? 'win' : 'loss';
}

function statusOf(match: StandingMatch): number {
  if (match.winnerId) return STATUS.completed;
  if (match.teamAId && match.teamBId) return STATUS.ready;
  if (match.teamAId || match.teamBId) return STATUS.waiting;
  return STATUS.locked;
}

// "Final", "Semifinals", "Quarterfinals", then "Round of N"
export function roundName(fractionOfFinal: number): string {
  if (fractionOfFinal === 1) return 'Final';
  if (fractionOfFinal === 0.5) return 'Semifinals';
  if (fractionOfFinal === 0.25) return 'Quarterfinals';
  return `Round of ${Math.round(2 / fractionOfFinal)}`;
}
