import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Format, formatHasGroups, STATUS_INFO, TournamentStatus } from '../../../shared/enums';
import { distributeTeams } from '../../tournaments/tournament-config';
import { StageTrack } from '../../tournaments/stage-track';
import { GroupTable } from '../group-table/group-table';
import { TeamManager } from '../team-manager/team-manager';
import { TournamentActions } from '../tournament-actions';
import { TournamentStore } from '../tournament.store';
import { currentRound, groupCards, roundCards, stageCounts, StatusAction, statusCard, topSlots } from './overview-status';

const ACTION_LABELS: Record<StatusAction, string> = {
  startGroups: 'Start groups',
  startBracket: 'Start bracket',
};

// Status card (where are we, the one next action), then teams or stage cards, facts beside them
@Component({
  selector: 'app-overview-tab',
  imports: [DatePipe, RouterLink, Button, GroupTable, StageTrack, TeamManager],
  templateUrl: './overview-tab.html',
  styleUrl: './overview-tab.css',
})
export class OverviewTab {
  readonly store = inject(TournamentStore);
  readonly actions = inject(TournamentActions);
  protected readonly actionLabels = ACTION_LABELS;

  readonly counts = computed(() => {
    const t = this.store.tournament();
    return t ? stageCounts(t, this.store.groups(), this.store.bracket()) : null;
  });

  // GroupsOnly with several groups has one winner per group
  private readonly champions = computed(() => this.store.results().filter((placement) => placement.placement === 1));

  readonly status = computed(() => {
    const t = this.store.tournament();
    const state = this.store.state();
    const counts = this.counts();
    return t && state && counts ? statusCard(t, state, counts, this.champions()) : null;
  });

  readonly statusColor = computed(() => {
    const t = this.store.tournament();
    return t ? `var(${STATUS_INFO[t.status].color})` : '';
  });

  // Filled as groups and the bracket decide places; the full list is on the Results tab
  readonly topSlots = computed(() => {
    const t = this.store.tournament();
    return t ? topSlots(t, this.store.groups(), this.store.bracket(), this.store.results(), this.store.teamNames()) : [];
  });

  readonly groupCards = computed(() => groupCards(this.store.groups()));
  readonly roundCards = computed(() => roundCards(this.store.bracket()));
  readonly currentRound = computed(() => currentRound(this.store.bracket(), this.store.teamNames()));

  readonly isSetup = computed(() => this.store.tournament()?.status === TournamentStatus.Setup);
  readonly isFinished = computed(() => this.store.tournament()?.status === TournamentStatus.Finished);
  readonly hasGroups = computed(() => {
    const t = this.store.tournament();
    return !!t && formatHasGroups(t.format);
  });
  readonly isGroupsAndBracket = computed(() => this.store.tournament()?.format === Format.GroupsAndBracket);
  // Cut line in the group tables
  readonly advancing = computed(() => (this.isGroupsAndBracket() ? this.store.tournament()!.advancingPerGroup : null));

  // The format in one plain sentence, e.g. "16 teams in 4 groups of 4, round robin, best of 3. The top 2 ..."
  readonly formatSummary = computed(() => {
    const t = this.store.tournament();
    if (!t) return '';
    const setup = t.status === TournamentStatus.Setup;
    const teams = setup ? `Up to ${t.maxTeams} teams` : `${t.currentTeams} teams`;
    const teamCount = setup ? t.maxTeams : t.currentTeams;

    if (t.format === Format.BracketOnly) return `${teams}, single elimination bracket, every match best of 3.`;

    const sizes = distributeTeams(teamCount, t.numberOfGroups);
    const min = Math.min(...sizes);
    const max = Math.max(...sizes);
    const groups =
      t.numberOfGroups === 1 ? 'one group' : `${t.numberOfGroups} groups of ${min === max ? min : `${min}-${max}`}`;
    const stage = `${teams} in ${groups}, round robin, every match best of 3.`;

    return t.format === Format.GroupsAndBracket
      ? `${stage} The top ${t.advancingPerGroup} of each group advance to a single elimination bracket.`
      : `${stage} The group position is the final result.`;
  });

  run(action: StatusAction): void {
    if (action === 'startGroups') this.actions.startGroups();
    else this.actions.startBracket();
  }
}
