import { DOCUMENT } from '@angular/common';
import { Component, computed, DestroyRef, effect, ElementRef, inject, signal, viewChild } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state';
import { roundName } from '../bracket-view/bracket-adapter';
import { BracketView, MatchClick } from '../bracket-view/bracket-view';
import { MatchPopup, PopupAnchor, popupAnchor } from '../match-popup/match-popup';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-bracket-tab',
  imports: [EmptyState, BracketView, MatchPopup],
  providers: [Scoring],
  templateUrl: './bracket-tab.html',
  styleUrl: './bracket-tab.css',
})
export class BracketTab {
  readonly store = inject(TournamentStore);

  readonly canScore = computed(() => !!this.store.state()?.canScoreMatches);
  readonly seeded = computed(() => !!this.store.bracket()?.isSeeded);
  // Up to 8 first-round matches (16 teams) fit one screen; bigger brackets scroll one half per step
  readonly halves = computed(
    () => (this.store.bracket()?.matches.filter((match) => match.round === 1).length ?? 0) > 8,
  );

  // The clicked match, scored in a popup at the click (as in the groups)
  readonly selectedMatchId = signal<number | null>(null);
  readonly anchor = signal<PopupAnchor | null>(null);
  private readonly board = viewChild<ElementRef<HTMLElement>>('board');
  private openedBy: MouseEvent | null = null;
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

  open(click: MatchClick): void {
    const board = this.board()?.nativeElement;
    if (!board) return;
    this.anchor.set(popupAnchor(board, click.element, click.event));
    this.selectedMatchId.set(click.matchId);
    this.openedBy = click.event;
  }

  // The click that just opened a match also reaches the popup's outside-click check: that one keeps it open
  close(event: MouseEvent | null = null): void {
    if (event && event === this.openedBy) return;
    this.selectedMatchId.set(null);
    this.anchor.set(null);
  }
}
