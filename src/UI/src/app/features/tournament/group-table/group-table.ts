import { Component, input } from '@angular/core';
import { Group } from '../../../core/models';
import { TEAM_STATUS_LABELS, TeamStatus } from '../../../shared/enums';

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
  // Small version for the Overview: no header row, no Games column, no status words (the row tint shows them)
  readonly compact = input(false);

  protected readonly statusLabels = TEAM_STATUS_LABELS;
  protected readonly advanced = TeamStatus.Advanced;
  protected readonly eliminated = TeamStatus.Eliminated;
  protected readonly champion = TeamStatus.Champion;
}
