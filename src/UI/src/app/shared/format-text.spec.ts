import { advanceText, groupSeedingText } from './format-text';

describe('format text', () => {
  it('says advance in singular for one team', () => {
    expect(advanceText(1)).toBe('The winner of each group advances');
    expect(advanceText(2)).toBe('The top 2 of each group advance');
  });

  it('explains group seeding only with several groups and several advancing teams', () => {
    expect(groupSeedingText(1, 2)).toBeNull();
    expect(groupSeedingText(4, 1)).toBeNull();
    expect(groupSeedingText(4, 2)).toContain('Group winners are seeded first');
  });
});
