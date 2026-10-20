import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { Format, FORMAT_LABELS, formatHasGroups, TournamentStatus } from '../../../shared/enums';import { TournamentActions } from '../tournament-actions';
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

  readonly playStage = computed(() => {
    const step = this.nextStep();
    return step.kind === 'play' ? step.stage : null;
  });
}
