import { groupsText } from './tournament-actions';

describe('groupsText', () => {
  it('names the group size when the teams split evenly', () => {
    expect(groupsText(16, 4)).toBe('4 groups of 4');
    expect(groupsText(5, 1)).toBe('1 group of 5');
  });

  it('lists the sizes when they differ', () => {
    expect(groupsText(10, 4)).toBe('4 groups (3, 3, 2, 2)');
  });
});
