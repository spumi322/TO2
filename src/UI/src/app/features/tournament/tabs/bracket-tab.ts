import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { EmptyState } from '../../../shared/empty-state';
import { roundName } from '../bracket-view/bracket-adapter';
import { BracketView } from '../bracket-view/bracket-view';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-bracket-tab',
  imports: [EmptyState, Button, BracketView, MatchCard],
  providers: [Scoring],
  templateUrl: './bracket-tab.html',
  styleUrl: './bracket-tab.css',
})
export class BracketTab {
  readonly store = inject(TournamentStore);
  readonly scoring = inject(Scoring);

  readonly canScore = computed(() => !!this.store.state()?.canScoreMatches);
  readonly seeded = computed(() => !!this.store.bracket()?.isSeeded);
  // Up to 8 first-round matches (16 teams) fit one screen; bigger brackets scroll one half per step
  readonly halves = computed(
    () => (this.store.bracket()?.matches.filter((match) => match.round === 1).length ?? 0) > 8,
  );

  // The clicked match, scored in the side panel with the same row as in the groups
  readonly selectedMatchId = signal<number | null>(null);
  readonly selectedMatch = computed(
    () => this.store.bracket()?.matches.find((match) => match.id === this.selectedMatchId()) ?? null,
  );
  readonly selectedRound = computed(() => {
    const match = this.selectedMatch();
    const rounds = Math.max(...(this.store.bracket()?.matches.map((m) => m.round ?? 1) ?? [1]));
    return match ? roundName(1 / 2 ** (rounds - (match.round ?? 1))) : '';
  });

  constructor() {
    const root = inject(DOCUMENT).documentElement;
    effect(() => root.classList.toggle('snap-blocks', this.halves()));
    inject(DestroyRef).onDestroy(() => root.classList.remove('snap-blocks'));
  }
}
