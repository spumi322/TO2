import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Popover } from 'primeng/popover';
import { EmptyState } from '../../../shared/empty-state';
import { BracketMatchClick, BracketView } from '../bracket-view/bracket-view';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-bracket-tab',
  imports: [Popover, EmptyState, BracketView, MatchCard],
  providers: [Scoring],
  templateUrl: './bracket-tab.html',
  styleUrl: './bracket-tab.css',
})
export class BracketTab {
  readonly store = inject(TournamentStore);
  readonly scoring = inject(Scoring);
  private readonly popover = viewChild.required(Popover);

  readonly canScore = computed(() => !!this.store.state()?.canScoreMatches);
  readonly seeded = computed(() => !!this.store.bracket()?.isSeeded);

  private readonly selectedMatchId = signal<number | null>(null);
  // Read from the store, so live updates show in the open popover
  readonly selectedMatch = computed(
    () => this.store.bracket()?.matches.find((match) => match.id === this.selectedMatchId()) ?? null,
  );

  open(click: BracketMatchClick): void {
    const match = this.store.bracket()?.matches.find((m) => m.id === click.matchId);
    // Nothing to score until both teams are known
    if (!match?.teamAId || !match.teamBId) return;

    this.selectedMatchId.set(click.matchId);
    this.popover().show(null, click.element ?? undefined);
  }
}
