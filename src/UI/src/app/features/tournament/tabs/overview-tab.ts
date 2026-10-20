import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Format, FORMAT_LABELS, formatHasGroups, TournamentStatus } from '../../../shared/enums';import { distributeTeams } from '../../tournaments/tournament-config';
import { TournamentActions } from '../tournament-actions';
import { TournamentStore } from '../tournament.store';

type NextStep =
  | { kind: 'addTeams' }
  | { kind: 'startGroups' }
  | { kind: 'startBracket' }
  | { kind: 'play'; stage: 'groups' | 'bracket' }
  | { kind: 'finished' }
  | { kind: 'none' };

@Component({
  selector: 'app-overview-tab',
  imports: [DatePipe, RouterLink, Button],
  templateUrl: './overview-tab.html',
  styleUrl: './overview-tab.css',
})
export class OverviewTab {
  readonly store = inject(TournamentStore);
  readonly actions = inject(TournamentActions);

  readonly formatLabel = computed(() => {
    const t = this.store.tournament();
    return t ? FORMAT_LABELS[t.format] : '';
  });
  readonly hasGroups = computed(() => {
    const t = this.store.tournament();
    return !!t && formatHasGroups(t.format);
  });
  readonly isGroupsAndBracket = computed(() => this.store.tournament()?.format === Format.GroupsAndBracket);
  readonly isCancelled = computed(() => this.store.tournament()?.status === TournamentStatus.Cancelled);

  // What the organizer does next, from the state flags (no status rules here)
  readonly nextStep = computed<NextStep>(() => {
    const t = this.store.tournament();
    const state = this.store.state();
    if (!t || !state) return { kind: 'none' };
    if (state.canStartGroups) return { kind: 'startGroups' };
    if (state.canStartBracket) return { kind: 'startBracket' };
    if (state.canModifyTeams) return { kind: 'addTeams' };
    if (state.canScoreMatches) {
      return { kind: 'play', stage: t.status === TournamentStatus.GroupsInProgress ? 'groups' : 'bracket' };
    }
    if (t.status === TournamentStatus.Finished) return { kind: 'finished' };
    return { kind: 'none' };
  });

  // Played / total per started stage (BYE matches are not played, so they don't count)
  readonly progress = computed(() => {
    const stages: { label: string; played: number; total: number }[] = [];
    const groupMatches = this.store.groups().flatMap((group) => group.matches);
    if (groupMatches.length) {
      stages.push({
        label: 'Group matches',
        played: groupMatches.filter((m) => m.winnerId !== null).length,
        total: groupMatches.length,
      });
    }
    const bracketMatches = (this.store.bracket()?.matches ?? []).filter(
      (m) => !(m.round === 1 && (!m.teamAId || !m.teamBId)),
    );
    if (this.store.bracket()?.isSeeded && bracketMatches.length) {
      stages.push({
        label: 'Bracket matches',
        played: bracketMatches.filter((m) => m.winnerId !== null).length,
        total: bracketMatches.length,
      });
    }
    return stages;
  });

  // GroupsOnly with several groups has one winner per group
  readonly champions = computed(() => this.store.results().filter((placement) => placement.placement === 1));

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

  readonly playStage = computed(() => {
    const step = this.nextStep();
    return step.kind === 'play' ? step.stage : null;
  });
}
