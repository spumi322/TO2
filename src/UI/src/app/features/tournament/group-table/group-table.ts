import { Component, computed, input } from '@angular/core';
import { Group } from '../../../core/models';
import { TeamStatus } from '../../../shared/enums';
import { teamForm } from '../tabs/groups-board';

// Teams arrive ranked by the server (GroupRanking); this only displays them
@Component({
  selector: 'app-group-table',
  templateUrl: './group-table.html',
  styleUrl: './group-table.css',
  host: { '[class.compact]': 'compact()' },
})
export class GroupTable {
  readonly group = input.required<Group>();
  // Draws the cut line under the last advancing position (GroupsAndBracket)
  readonly advancing = input<number | null>(null);
  // Small version for the Overview: no header row, no form, no game difference
  readonly compact = input(false);

  readonly rows = computed(() => {
    const group = this.group();
    // GroupsOnly (no advancing): the first team of a finished group is its winner, shown like an advanced team
    const showWinner = group.isFinished && this.advancing() === null;
    return group.teams.map((team, index) => {
      const difference = team.gameWins - team.gameLosses;
      return {
        team,
        form: teamForm(group, team.id),
        difference: difference > 0 ? `+${difference}` : `${difference}`,
        groupWinner: showWinner && index === 0,
      };
    });
  });

  protected readonly advanced = TeamStatus.Advanced;
  protected readonly eliminated = TeamStatus.Eliminated;
}
