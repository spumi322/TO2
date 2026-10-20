import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state';
import { BracketTeamClick, BracketView } from '../bracket-view/bracket-view';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-bracket-tab',
  imports: [EmptyState, BracketView],
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

  constructor() {
    const root = inject(DOCUMENT).documentElement;
    effect(() => root.classList.toggle('snap-blocks', this.halves()));
    inject(DestroyRef).onDestroy(() => root.classList.remove('snap-blocks'));
  }

  // Same as a match card in the groups: the clicked team wins the next unplayed game
  score(click: BracketTeamClick): void {
    const match = this.store.bracket()?.matches.find((m) => m.id === click.matchId);
    if (!match?.teamAId || !match.teamBId || match.winnerId !== null) return;

    const game = [...match.games].sort((a, b) => a.id - b.id).find((g) => g.winnerId === null);
    if (game) void this.scoring.setWinner({ match, game, winnerId: click.teamId });
  }
}
