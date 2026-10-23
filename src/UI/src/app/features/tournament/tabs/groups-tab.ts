import { Component, computed, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { EmptyState } from '../../../shared/empty-state';
import { Format } from '../../../shared/enums';
import { GroupTable } from '../group-table/group-table';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';
import { matchLines } from './groups-board';

// All groups on one page: each card has its table and its matches; a clicked match is scored in the panel beside them
@Component({
  selector: 'app-groups-tab',
  imports: [EmptyState, Button, GroupTable, MatchCard],
  providers: [Scoring],
  templateUrl: './groups-tab.html',
  styleUrl: './groups-tab.css',
  host: { '(document:keydown.escape)': 'selectedMatchId.set(null)' },
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

  // Match lines per group id
  readonly lines = computed(
    () => new Map(this.store.groups().map((group) => [group.id, matchLines(group, this.store.teamNames(), this.store.drafts())])),
  );

  private readonly matches = computed(() => this.store.groups().flatMap((group) => group.matches));

  // "16 of 24 matches played · Top 2 of each group advance · Best of 3 · Ranked by match wins, then game difference"
  readonly summary = computed(() => {
    const matches = this.matches();
    const played = matches.filter((match) => match.winnerId !== null).length;
    const advancing = this.advancing();
    return [
      `${played} of ${matches.length} matches played`,
      ...(advancing ? [`Top ${advancing} of each group advance`] : []),
      ...(matches.length ? [`Best of ${matches[0].bestOf}`] : []),
      'Ranked by match wins, then game difference',
    ].join(' · ');
  });

  readonly selectedMatchId = signal<number | null>(null);
  readonly selected = computed(() => {
    const id = this.selectedMatchId();
    const group = this.store.groups().find((g) => g.matches.some((match) => match.id === id));
    const match = group?.matches.find((m) => m.id === id);
    return group && match ? { match, title: `${group.name} · Round ${match.round ?? 1}` } : null;
  });

  played(matches: { winnerId: number | null }[]): number {
    return matches.filter((match) => match.winnerId !== null).length;
  }
}
