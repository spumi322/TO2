import { Component, input } from '@angular/core';
import { Group } from '../../../core/models';
import { TEAM_STATUS_LABELS, TeamStatus } from '../../../shared/enums';

// Teams arrive ranked by the server (GroupRanking); this only displays them
@Component({
  selector: 'app-group-table',
  templateUrl: './group-table.html',
  styleUrl: './group-table.css',
})
export class GroupTable {
  readonly group = input.required<Group>();
  // Draws the cut line under the last advancing position (GroupsAndBracket)
  readonly advancing = input<number | null>(null);

  protected readonly statusLabels = TEAM_STATUS_LABELS;
  protected readonly advanced = TeamStatus.Advanced;
  protected readonly eliminated = TeamStatus.Eliminated;
  protected readonly champion = TeamStatus.Champion;
}
