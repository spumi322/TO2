import { Format } from '../../shared/enums';
import {
  bracketTeamCount,
  bracketTreeRounds,
  buildCreateRequest,
  computeBracketSize,
  computeByeCount,
  distributeTeams,
  generateMatchups,
  nextPowerOfTwo,
  previewConfig,
  TournamentConfig,
  validateConfig,
} from './tournament-config';

const config = (overrides: Partial<TournamentConfig> = {}): TournamentConfig => ({
  name: 'Summer Cup',
  description: '',
  maxTeams: 8,
  format: Format.GroupsAndBracket,
  numberOfGroups: 2,
  advancingPerGroup: 2,
  ...overrides,
});

describe('tournament config', () => {
  it('rounds up to the next power of two', () => {
    expect([1, 2, 3, 5, 8, 9, 32].map(nextPowerOfTwo)).toEqual([2, 2, 4, 8, 8, 16, 32]);
  });

  it('splits teams evenly, extra teams to the first groups', () => {
    expect(distributeTeams(8, 2)).toEqual([4, 4]);
    expect(distributeTeams(10, 3)).toEqual([4, 3, 3]);
    expect(distributeTeams(8, 0)).toEqual([]);
  });

  it('sizes the bracket from groups x advancing, or from max teams', () => {
    expect(bracketTeamCount(config({ numberOfGroups: 3, advancingPerGroup: 2 }))).toBe(6);
    expect(bracketTeamCount(config({ format: Format.BracketOnly, maxTeams: 12 }))).toBe(12);
    expect(bracketTeamCount(config({ format: Format.GroupsOnly }))).toBe(0);
    expect(computeBracketSize(6)).toBe(8);
    expect(computeByeCount(8, 6)).toBe(2);
  });

  it('pairs seed i with seed N+1-i and keeps top seeds apart', () => {
    expect(generateMatchups(4)).toEqual([
      [1, 4],
      [2, 3],
    ]);
    expect(generateMatchups(8)).toEqual([
      [1, 8],
      [4, 5],
      [2, 7],
      [3, 6],
    ]);
    expect(generateMatchups(6)).toEqual([]);
  });

  it('previews groups and bracket', () => {
    const preview = previewConfig(config({ maxTeams: 10, numberOfGroups: 3, advancingPerGroup: 2 }));

    expect(preview.groupSizes).toEqual([4, 3, 3]);
    expect(preview.groupMatches).toBe(6 + 3 + 3);
    expect(preview).toMatchObject({ bracketTeams: 6, bracketSize: 8, byes: 2, rounds: 3 });
  });

  it('builds the bracket tree: seeds and BYEs in round 1, empty slots after, pairs feeding one match', () => {
    const tree = bracketTreeRounds(generateMatchups(8), 6);

    expect(tree.map((round) => round.map((pair) => pair.length))).toEqual([[2, 2], [2], [1]]);
    const first = tree[0].flat();
    expect(first.map((m) => [m.top.label, m.bottom.label])).toEqual([
      ['#1', 'BYE'],
      ['#4', '#5'],
      ['#2', 'BYE'],
      ['#3', '#6'],
    ]);
    expect(first[0].top).toMatchObject({ byeOpponent: true, bye: false });
    expect(first[0].bottom).toMatchObject({ byeOpponent: false, bye: true });
    expect(first[1].top.byeOpponent).toBe(false);
    expect(tree[2][0][0].top.label).toBe('');
  });

  it('shows no seed numbers for a random draw', () => {
    const labels = bracketTreeRounds(generateMatchups(8), 6, false)[0].flat().flatMap((m) => [m.top.label, m.bottom.label]);

    expect(labels).toEqual(['Team', 'BYE', 'Team', 'Team', 'Team', 'BYE', 'Team', 'Team']);
  });

  it('mirrors the server validation rules', () => {
    expect(validateConfig(config())).toEqual({});
    expect(validateConfig(config({ name: ' ab ' })).name).toBeDefined();
    expect(validateConfig(config({ maxTeams: 33 })).maxTeams).toBeDefined();
    expect(validateConfig(config({ numberOfGroups: 3 })).numberOfGroups).toBeDefined(); // 8 / 3 = 2 per group
    expect(validateConfig(config({ advancingPerGroup: 4 })).advancingPerGroup).toBeDefined(); // nobody eliminated
    expect(validateConfig(config({ numberOfGroups: 1, advancingPerGroup: 1 })).advancingPerGroup).toBeDefined();
    expect(validateConfig(config({ format: Format.BracketOnly, numberOfGroups: null }))).toEqual({});
  });

  it('reports an empty Max teams only once, not as a group size problem', () => {
    const empty = null as unknown as number;
    expect(validateConfig(config({ maxTeams: empty }))).toEqual({ maxTeams: 'Max teams must be between 4 and 32.' });
    expect(validateConfig(config({ format: Format.GroupsOnly, maxTeams: empty, advancingPerGroup: null }))).toEqual({
      maxTeams: 'Max teams must be between 4 and 32.',
    });
  });

  it('sends only the fields of the chosen format', () => {
    expect(buildCreateRequest(config({ format: Format.BracketOnly, description: '  ' }))).toEqual({
      name: 'Summer Cup',
      description: null,
      maxTeams: 8,
      format: Format.BracketOnly,
      numberOfGroups: null,
      advancingPerGroup: null,
    });
    expect(buildCreateRequest(config({ format: Format.GroupsOnly }))).toMatchObject({
      numberOfGroups: 2,
      advancingPerGroup: null,
    });
  });
});
