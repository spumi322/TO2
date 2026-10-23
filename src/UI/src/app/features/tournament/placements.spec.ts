import { TeamPlacement } from '../../core/models';
import { TeamStatus } from '../../shared/enums';
import { ordinal, podiumTiers, toPlacementRows, topPlacements } from './placements';

const p = (teamName: string, placement: number, eliminatedInRound = 0, groupName: string | null = null): TeamPlacement => ({
  teamId: teamName.length,
  teamName,
  placement,
  status: placement === 1 ? TeamStatus.Champion : TeamStatus.Eliminated,
  eliminatedInRound,
  groupName,
});

describe('placements', () => {
  it('writes ordinals', () => {
    expect([1, 2, 3, 4, 11, 12, 13, 21, 22].map(ordinal)).toEqual([
      '1st', '2nd', '3rd', '4th', '11th', '12th', '13th', '21st', '22nd',
    ]);
  });

  it('shows shared ranks as ranges and how far each team got', () => {
    // 8-team bracket (3 rounds) after a group stage
    const rows = toPlacementRows(
      [p('Alpha', 1), p('Bravo', 2, 3), p('Charlie', 3, 2), p('Delta', 3, 2), p('Echo', 5, 1), p('Fox', 9, 0, 'Group B')],
      3,
    );

    expect(rows.map((r) => [r.teamName, r.rankLabel, r.reached])).toEqual([
      ['Alpha', '1st', 'Champion'],
      ['Bravo', '2nd', 'Final'],
      ['Charlie', '3rd-4th', 'Semifinals'],
      ['Delta', '3rd-4th', 'Semifinals'],
      ['Echo', '5th', 'Quarterfinals'],
      ['Fox', '9th', 'Group B'],
    ]);
  });

  it('shows GroupsOnly placements as positions in their group', () => {
    const rows = toPlacementRows(
      [p('Alpha', 1, 0, 'Group A'), p('Bravo', 1, 0, 'Group B'), p('Charlie', 2, 0, 'Group A'), p('Delta', 2, 0, 'Group B')],
      0,
      true,
    );

    expect(rows.map((r) => [r.rankLabel, r.reached])).toEqual([
      ['1st', 'Group A winner'],
      ['1st', 'Group B winner'],
      ['2nd', 'Group A'],
      ['2nd', 'Group B'],
    ]);
  });

  it('keeps shared ranks together on the podium', () => {
    const tiers = podiumTiers([p('Alpha', 1), p('Bravo', 2), p('Charlie', 3), p('Delta', 3), p('Echo', 5)]);

    expect(tiers.map((tier) => tier.map((t) => t.teamName))).toEqual([['Alpha'], ['Bravo'], ['Charlie', 'Delta']]);
  });

  it('lists whole tiers until 4 teams for the Overview', () => {
    const bracket = toPlacementRows([p('Alpha', 1), p('Bravo', 2), p('Charlie', 3), p('Delta', 3), p('Echo', 5), p('Fox', 5)], 3);
    expect(topPlacements(bracket).map((r) => r.teamName)).toEqual(['Alpha', 'Bravo', 'Charlie', 'Delta']);

    // GroupsOnly, 5 groups: all group winners, no runner-up
    const winners = ['A', 'B', 'C', 'D', 'E'].map((g) => p(`Team ${g}`, 1, 0, `Group ${g}`));
    const groupsOnly = toPlacementRows([...winners, p('Second', 2, 0, 'Group A')], 0, true);
    expect(topPlacements(groupsOnly)).toHaveLength(5);
  });
});
