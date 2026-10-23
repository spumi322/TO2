import { Group, MatchDraft, StandingMatch } from '../../../core/models';

export type MatchLineState = 'done' | 'draft' | 'open';

// One match on a group card: "Alpha 2–1 Bravo"; a draft shows its score until it is confirmed
export interface MatchLine {
  id: number;
  round: number;
  teamA: string;
  teamB: string;
  score: string;
  state: MatchLineState;
  winner: 'A' | 'B' | null;
}

// Round order, then id: a match never moves while it is being scored
const inPlayOrder = (matches: StandingMatch[]) =>
  [...matches].sort((a, b) => (a.round ?? 1) - (b.round ?? 1) || a.id - b.id);

// The team's last results, oldest first
export function teamForm(group: Group, teamId: number, last = 5): ('W' | 'L')[] {
  return inPlayOrder(group.matches)
    .filter((match) => match.winnerId !== null && (match.teamAId === teamId || match.teamBId === teamId))
    .map((match): 'W' | 'L' => (match.winnerId === teamId ? 'W' : 'L'))
    .slice(-last);
}

export function matchLines(group: Group, teamNames: Map<number, string>, drafts: ReadonlyMap<number, MatchDraft>): MatchLine[] {
  return inPlayOrder(group.matches).map((match) => {
    const decided = match.winnerId !== null;
    const draft = decided ? undefined : drafts.get(match.id);
    const state: MatchLineState = decided ? 'done' : draft ? 'draft' : 'open';
    const wins = draft ?? match;
    return {
      id: match.id,
      round: match.round ?? 1,
      teamA: teamNames.get(match.teamAId ?? 0) ?? '',
      teamB: teamNames.get(match.teamBId ?? 0) ?? '',
      score: state === 'open' ? 'vs' : `${wins.teamAWins}–${wins.teamBWins}`,
      state,
      winner: !decided ? null : match.winnerId === match.teamAId ? 'A' : 'B',
    };
  });
}
