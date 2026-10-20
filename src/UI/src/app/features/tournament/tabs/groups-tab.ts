import { Component, computed, inject } from '@angular/core';
import { StandingMatch } from '../../../core/models';
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
  readonly advancing = computed(() => {
    const t = this.store.tournament();
    return t?.format === Format.GroupsAndBracket ? t.advancingPerGroup : null;
  });

  // Stable order (by round): a card never jumps away while it is being scored
  sortedMatches(matches: StandingMatch[]): StandingMatch[] {
    return [...matches].sort((a, b) => (a.round ?? 0) - (b.round ?? 0) || a.id - b.id);
  }
}
