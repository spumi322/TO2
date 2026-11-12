import { Component, computed, ElementRef, inject, input, output } from '@angular/core';
import { StandingMatch } from '../../../core/models';
import { MatchCard } from '../match-card/match-card';
import { Scoring } from '../scoring';
import { TournamentStore } from '../tournament.store';

// Where the popup opens, in pixels of its positioned container
export interface PopupAnchor {
  top: number;
  left: number;
  above: boolean;
}

const WIDTH = 340;

// Centered on the click, under the clicked match (above it near the bottom of the screen), kept inside the container
export function popupAnchor(container: HTMLElement, clicked: HTMLElement, event: MouseEvent): PopupAnchor {
  const box = container.getBoundingClientRect();
  const rect = clicked.getBoundingClientRect();
  const width = Math.min(WIDTH, box.width);
  // Keyboard clicks have no pointer position: use the middle of the match
  const x = event.detail > 0 ? event.clientX : rect.left + rect.width / 2;
  const above = rect.bottom + 240 > window.innerHeight && rect.top > 240;
  return {
    top: (above ? rect.top - 6 : rect.bottom + 6) - box.top,
    left: Math.max(0, Math.min(x - box.left - width / 2, box.width - width)),
    above,
  };
}

/**
 * A match scored where it was clicked (groups and bracket): the tab gives the match and the anchor.
 * Closes on Esc, on a click outside, and once the result is saved. The click is passed on, so the tab can keep
 * the popup open when that same click opened another match.
 */
@Component({
  selector: 'app-match-popup',
  imports: [MatchCard],
  template: `
    <div class="row">
      <h3>{{ title() }}</h3>
      <span class="muted small">Best of {{ match().bestOf }}</span>
    </div>
    <app-match-card
      [match]="match()"
      [teamNames]="store.teamNames()"
      [draft]="store.drafts().get(match().id)"
      [canScore]="canScore()"
      [busy]="scoring.savingMatchId() === match().id"
      (draftChange)="store.setDraft($event.match, $event.teamAWins, $event.teamBWins)"
      (confirm)="confirm($event)"
    />
  `,
  styles: `
    :host {
      position: absolute;
      z-index: 4;
      width: min(340px, 100%);
      display: flex;
      flex-direction: column;
      gap: 10px;
      box-shadow: 0 12px 32px rgb(0 0 0 / 0.35);
    }
    :host(.above) {
      transform: translateY(-100%);
    }
    h3 {
      margin: 0;
    }
  `,
  host: {
    class: 'card',
    role: 'dialog',
    '[attr.aria-label]': 'title()',
    '[class.above]': 'anchor().above',
    '[style.top.px]': 'anchor().top',
    '[style.left.px]': 'anchor().left',
    '(document:keydown.escape)': 'closed.emit(null)',
    '(document:click)': 'onDocumentClick($event)',
  },
})
export class MatchPopup {
  readonly match = input.required<StandingMatch>();
  readonly title = input.required<string>();
  readonly anchor = input.required<PopupAnchor>();
  readonly canScore = input(false);
  readonly closed = output<MouseEvent | null>();

  readonly store = inject(TournamentStore);
  readonly scoring = inject(Scoring);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  private readonly decided = computed(() => {
    const id = this.match().id;
    const matches = [...this.store.groups().flatMap((group) => group.matches), ...(this.store.bracket()?.matches ?? [])];
    return matches.find((match) => match.id === id)?.winnerId != null;
  });

  async confirm(match: StandingMatch): Promise<void> {
    await this.scoring.confirm(match);
    if (this.decided()) this.closed.emit(null);
  }

  protected onDocumentClick(event: MouseEvent): void {
    if (event.target instanceof Node && this.host.nativeElement.contains(event.target)) return;
    this.closed.emit(event);
  }
}
