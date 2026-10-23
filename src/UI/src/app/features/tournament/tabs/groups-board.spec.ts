import { Group, MatchDraft, StandingMatch } from '../../../core/models';
import { matchLines, teamForm } from './groups-board';

const match = (id: number, round: number, a: number, b: number, wins: [number, number] | null = null): StandingMatch =>
  ({
    id,
    round,
    teamAId: a,
    teamBId: b,
    teamAWins: wins?.[0] ?? 0,
    teamBWins: wins?.[1] ?? 0,
    winnerId: wins ? (wins[0] > wins[1] ? a : b) : null,
  }) as StandingMatch;

const group = (matches: StandingMatch[]): Group => ({ matches }) as Group;
const names = new Map([[1, 'Alpha'], [2, 'Bravo'], [3, 'Charlie']]);

describe('groups board', () => {
  // 3 teams: one team rests each round
  const odd = group([
    match(12, 2, 1, 3, [0, 2]),
    match(10, 1, 2, 3),
    match(11, 1, 1, 2, [2, 1]),
  ]);

  it('lists the decided results of a team in round order', () => {
    expect(teamForm(odd, 1)).toEqual(['W', 'L']);
    expect(teamForm(odd, 3)).toEqual(['W']);
    expect(teamForm(odd, 2)).toEqual(['L']);
  });

  it('keeps the last results only', () => {
    const many = group([1, 2, 3, 4, 5, 6].map((round) => match(round, round, 1, 2, round % 2 ? [2, 0] : [0, 2])));
    expect(teamForm(many, 1, 3)).toEqual(['L', 'W', 'L']);
  });

  it('turns matches into lines in play order, a draft showing its score', () => {
    const drafts = new Map<number, MatchDraft>([[10, { teamAWins: 1, teamBWins: 0, by: 'anna' }]]);

    expect(matchLines(odd, names, drafts)).toEqual([
      { id: 10, round: 1, teamA: 'Bravo', teamB: 'Charlie', score: '1–0', state: 'draft', winner: null },
      { id: 11, round: 1, teamA: 'Alpha', teamB: 'Bravo', score: '2–1', state: 'done', winner: 'A' },
      { id: 12, round: 2, teamA: 'Alpha', teamB: 'Charlie', score: '0–2', state: 'done', winner: 'B' },
    ]);
  });

  it('shows vs for a match nobody is scoring, and ignores a draft once decided', () => {
    const drafts = new Map<number, MatchDraft>([[11, { teamAWins: 1, teamBWins: 1, by: null }]]);
    const lines = matchLines(odd, names, drafts);

    expect(lines[0]).toMatchObject({ score: 'vs', state: 'open' });
    expect(lines[1]).toMatchObject({ score: '2–1', state: 'done' });
  });
});
