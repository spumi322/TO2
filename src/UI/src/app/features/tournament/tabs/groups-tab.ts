import { Component, computed, ElementRef, inject, signal, viewChild } from '@angular/core';
import { Button } from 'primeng/button';
import { StandingMatch } from '../../../core/models';
import { EmptyState } from '../../../shared/empty-state';
import { Format } from '../../../shared/enums';
import { GroupTable } from '../group-table/group-table';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';
import { matchLines } from './groups-board';

// All groups on one page: each card has its table and its matches; a clicked match is scored in a panel that pops up at it
@Component({
  selector: 'app-groups-tab',
  imports: [EmptyState, Button, GroupTable, MatchCard],
  providers: [Scoring],
  templateUrl: './groups-tab.html',
  styleUrl: './groups-tab.css',
  host: { '(document:keydown.escape)': 'close()', '(document:click)': 'closeOutside($event)' },
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
  // Where the panel pops up, in board pixels: centered on the click, under the line (above it near the bottom of the screen)
  readonly anchor = signal<{ top: number; left: number; above: boolean } | null>(null);
  private readonly board = viewChild<ElementRef<HTMLElement>>('board');
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
    const line = event.currentTarget as HTMLElement;
    if (!board) return;
    const box = board.getBoundingClientRect();
    const rect = line.getBoundingClientRect();
    const panelWidth = Math.min(340, box.width);
    // Keyboard clicks have no pointer position: use the middle of the line
    const x = event.detail > 0 ? event.clientX : rect.left + rect.width / 2;
    const above = rect.bottom + 240 > window.innerHeight && rect.top > 240;
    this.anchor.set({
      top: (above ? rect.top : rect.bottom) - box.top + (above ? -6 : 6),
      left: Math.max(0, Math.min(x - box.left - panelWidth / 2, box.width - panelWidth)),
      above,
    });
    this.selectedMatchId.set(matchId);
  }

  // Closes once the result is saved (the match has its winner then)
  async confirm(match: StandingMatch): Promise<void> {
    await this.scoring.confirm(match);
    if (this.selected()?.match.winnerId != null) this.close();
  }

  // A click anywhere but the panel or a match line closes the panel
  closeOutside(event: MouseEvent): void {
    if (!(event.target as Element | null)?.closest('.panel, .line')) this.close();
  }

  close(): void {
    this.selectedMatchId.set(null);
    this.anchor.set(null);
  }
}
