import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state';
import { Format } from '../../../shared/enums';
import { GroupTable } from '../group-table/group-table';
import { MatchPopup, PopupAnchor, popupAnchor } from '../match-popup/match-popup';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';
import { matchLines } from './groups-board';

// All groups on one page: each card has its table and its matches; a clicked match is scored in a popup at the click
@Component({
  selector: 'app-groups-tab',
  imports: [EmptyState, GroupTable, MatchPopup],
  providers: [Scoring],
  templateUrl: './groups-tab.html',
  styleUrl: './groups-tab.css',
})
export class GroupsTab {
  readonly store = inject(TournamentStore);

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

  // "16 of 24 matches played · Top 2 of each group advance · Best of 3 · Ranked by match wins, game difference, then head-to-head"
  readonly summary = computed(() => {
    const matches = this.matches();
    const played = matches.filter((match) => match.winnerId !== null).length;
    const advancing = this.advancing();
    return [
      `${played} of ${matches.length} matches played`,
      ...(advancing ? [`Top ${advancing} of each group advance`] : []),
      ...(matches.length ? [`Best of ${matches[0].bestOf}`] : []),
      'Ranked by match wins, game difference, then head-to-head',
    ].join(' · ');
  });

  readonly selectedMatchId = signal<number | null>(null);
  readonly anchor = signal<PopupAnchor | null>(null);
  private readonly board = viewChild<ElementRef<HTMLElement>>('board');
  private openedBy: MouseEvent | null = null;
  readonly selected = computed(() => {
    const id = this.selectedMatchId();
    const group = this.store.groups().find((g) => g.matches.some((match) => match.id === id));
    const match = group?.matches.find((m) => m.id === id);
    return group && match ? { match, title: `${group.name} · Round ${match.round ?? 1}` } : null;
  });

  played(matches: { winnerId: number | null }[]): number {
    return matches.filter((match) => match.winnerId !== null).length;
  }

  open(matchId: number, event: MouseEvent): void {
    const board = this.board()?.nativeElement;
    if (!board) return;
    this.anchor.set(popupAnchor(board, event.currentTarget as HTMLElement, event));
    this.selectedMatchId.set(matchId);
    this.openedBy = event;
  }

  // The click that just opened a match also reaches the popup's outside-click check: that one keeps it open
  close(event: MouseEvent | null = null): void {
    if (event && event === this.openedBy) return;
    this.selectedMatchId.set(null);
    this.anchor.set(null);
  }
}
