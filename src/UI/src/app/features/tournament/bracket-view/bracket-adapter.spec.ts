import { Bracket, StandingGame, StandingMatch } from '../../../core/models';
import { roundName, toViewerData } from './bracket-adapter';

const names = new Map([
  [1, 'Alpha'],
  [2, 'Bravo'],
  [3, 'Charlie'],
]);

function match(partial: Partial<StandingMatch>): StandingMatch {
  return {
    id: 0,
    standingId: 9,
    round: 1,
    seed: 1,
    teamAId: null,
    teamBId: null,
    winnerId: null,
    loserId: null,
    bestOf: 3,
    version: 0,
    teamAWins: 0,
    teamBWins: 0,
    games: [],
    ...partial,
  };
}

function game(id: number, matchId: number, winnerId: number | null): StandingGame {
  return { id, matchId, teamAId: 2, teamBId: 3, winnerId, version: 0 };
}

// 3 teams in a 4-slot bracket: Alpha has a BYE, Bravo vs Charlie is played 1-1, the final waits for its winner
const bracket: Bracket = {
  id: 9,
  name: 'Bracket',
  isFinished: false,
  isSeeded: true,
  version: 0,
  matches: [
    match({ id: 100, round: 1, seed: 1, teamAId: 1, winnerId: 1 }),
    match({
      id: 101,
      round: 1,
      seed: 2,
      teamAId: 2,
      teamBId: 3,
      teamAWins: 1,
      teamBWins: 1,
      games: [game(501, 101, 3), game(500, 101, 2), game(502, 101, null)],
    }),
    match({ id: 102, round: 2, seed: 1, teamAId: 1 }),
  ],
};

describe('toViewerData', () => {
  const data = toViewerData(bracket, names, 77);

  it('makes one single elimination stage', () => {
    expect(data.stages).toEqual([expect.objectContaining({ id: 9, tournament_id: 77, type: 'single_elimination' })]);
  });

  it('maps rounds and positions', () => {
    expect(data.matches.map((m) => [m.id, m.round_id, m.number])).toEqual([
      [100, 1, 1],
      [101, 1, 2],
      [102, 2, 1],
    ]);
  });

  it('shows an empty round 1 slot as a BYE and a later empty slot as TBD', () => {
    const [bye, , final] = data.matches;
    expect(bye.opponent2).toBeNull();
    expect(bye.opponent1).toEqual(expect.objectContaining({ id: 1, result: 'win' }));
    expect(final.opponent2).toEqual({ id: null });
  });

  it('carries the series score and the winner', () => {
    const played = data.matches[1];
    expect(played.opponent1).toEqual(expect.objectContaining({ id: 2, score: 1 }));
    expect(played.opponent2).toEqual(expect.objectContaining({ id: 3, score: 1 }));
    expect(played.child_count).toBe(3);
    expect(data.matches[0].status).toBe(4);
    expect(played.status).toBe(2);
    expect(data.matches[2].status).toBe(1);
  });

  it('numbers games in order under their match', () => {
    expect(data.matchGames.map((g) => [g.id, g.parent_id, g.number])).toEqual([
      [500, 101, 1],
      [501, 101, 2],
      [502, 101, 3],
    ]);
    expect(data.matchGames[1].opponent2).toEqual(expect.objectContaining({ id: 3, result: 'win' }));
  });

  it('lists every known team once with its name', () => {
    expect(data.participants.map((p) => p.name).sort()).toEqual(['Alpha', 'Bravo', 'Charlie']);
  });

  it('shows an open match with a draft as running with the draft score, a decided one keeps its result', () => {
    const drafts = new Map([
      [101, { teamAWins: 2, teamBWins: 1, by: 'anna' }],
      [100, { teamAWins: 1, teamBWins: 0, by: 'anna' }],
    ]);
    const [bye, drafted] = toViewerData(bracket, names, 77, drafts).matches;
    expect(drafted.status).toBe(3);
    expect(drafted.opponent1).toEqual(expect.objectContaining({ id: 2, score: 2 }));
    expect(drafted.opponent2).toEqual(expect.objectContaining({ id: 3, score: 1 }));
    expect(bye.status).toBe(4);
  });
});

describe('roundName', () => {
  it('names the last rounds and counts the rest', () => {
    expect([1, 0.5, 0.25, 0.125].map(roundName)).toEqual(['Final', 'Semifinals', 'Quarterfinals', 'Round of 16']);
  });
});
