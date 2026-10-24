import { checkTeamNames, parseTeamNames } from './team-names';

describe('team names', () => {
  it('splits on commas and new lines, trims, drops empty entries', () => {
    expect(parseTeamNames(' Alpha, Bravo ,,\nCharlie\n ')).toEqual(['Alpha', 'Bravo', 'Charlie']);
  });

  it('finds long, duplicate and registered names ignoring case, and capacity', () => {
    const problems = checkTeamNames(['Alpha', 'ALPHA', 'bravo', 'x'.repeat(21)], ['Bravo'], 3);

    expect(problems).toEqual([
      `Longer than 20 characters: ${'x'.repeat(21)}`,
      'Listed twice: ALPHA',
      'Already registered: bravo',
      'Only 3 slots left.',
    ]);
  });

  it('accepts valid names', () => {
    expect(checkTeamNames(['Alpha', 'Bravo'], ['Charlie'], 2)).toEqual([]);
  });
});
