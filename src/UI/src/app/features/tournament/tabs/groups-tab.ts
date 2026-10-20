import { Component, computed, DestroyRef, effect, inject } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { Group, StandingMatch } from '../../../core/models';
import { EmptyState } from '../../../shared/empty-state';
import { Format } from '../../../shared/enums';
import { GroupTable } from '../group-table/group-table';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-groups-tab',
  imports: [EmptyState, GroupTable, MatchCard],
  providers: [Scoring],
  templateUrl: './groups-tab.html',
  styleUrl: './groups-tab.css',
})
export class GroupsTab {
  readonly store = inject(TournamentStore);
  readonly scoring = inject(Scoring);

  readonly canScore = computed(() => !!this.store.state()?.canScoreMatches);
  readonly seeded = computed(() => this.store.groups().some((group) => group.isSeeded));
  readonly single = computed(() => this.store.groups().length === 1);
  readonly advancing = computed(() => {
    const t = this.store.tournament();
    return t?.format === Format.GroupsAndBracket ? t.advancingPerGroup : null;
  });

  constructor() {
    // Two groups per screen; with more, each scroll step moves a whole row of groups
    const root = inject(DOCUMENT).documentElement;
    effect(() => root.classList.toggle('snap-blocks', this.store.groups().length > 2));
    inject(DestroyRef).onDestroy(() => root.classList.remove('snap-blocks'));
  }

  // How the table is read, in one line above it
  readonly rule = computed(() => {
    const advancing = this.advancing();
    const ranking = 'Ranked by match wins, then game difference.';
    return advancing ? `Top ${advancing} advance. ${ranking}` : ranking;
  });

  played(group: Group): number {
    return group.matches.filter((match) => match.winnerId !== null).length;
  }

  // Matches under "Round 1", "Round 2", ... in play order (a match never moves while it is being scored)
  rounds(group: Group): { round: number; matches: StandingMatch[] }[] {
    const byRound = new Map<number, StandingMatch[]>();
    for (const match of [...group.matches].sort((a, b) => a.id - b.id)) {
      const round = match.round ?? 1;
      byRound.set(round, [...(byRound.get(round) ?? []), match]);
    }
    return [...byRound.entries()].sort(([a], [b]) => a - b).map(([round, matches]) => ({ round, matches }));
  }
}
