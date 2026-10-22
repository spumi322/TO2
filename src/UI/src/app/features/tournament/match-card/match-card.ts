import { Component, computed, input, output } from '@angular/core';
import { MatchDraft, StandingMatch } from '../../../core/models';

export interface DraftChange {
  match: StandingMatch;
  teamAWins: number;
  teamBWins: number;
}

type Side = 'a' | 'b';

/**
 * One match (groups and bracket). While the match is open, each team has a − / + for its game wins:
 * that is a draft, shared live but not saved. When one team has the wins needed, Confirm saves the result.
 * A confirmed match only shows its score.
 */
@Component({
  selector: 'app-match-card',
  templateUrl: './match-card.html',
  styleUrl: './match-card.css',
})
export class MatchCard {
  readonly match = input.required<StandingMatch>();
  readonly teamNames = input.required<Map<number, string>>();
  readonly draft = input<MatchDraft | null | undefined>(null);
  readonly canScore = input(false);
  readonly busy = input(false);
  // One line per team (bracket panel) instead of one row
  readonly stacked = input(false);
  readonly draftChange = output<DraftChange>();
  readonly confirm = output<StandingMatch>();

  // Best of 3 needs 2 wins, best of 5 needs 3
  readonly winsNeeded = computed(() => Math.floor(this.match().bestOf / 2) + 1);
  readonly confirmed = computed(() => this.match().winnerId !== null);
  readonly editable = computed(
    () => this.canScore() && !this.confirmed() && !!this.match().teamAId && !!this.match().teamBId,
  );

  // Saved score once confirmed; before that the draft (or games saved by the old per-game scoring)
  readonly score = computed(() => {
    const m = this.match();
    const draft = this.draft();
    return !this.confirmed() && draft ? { a: draft.teamAWins, b: draft.teamBWins } : { a: m.teamAWins, b: m.teamBWins };
  });
  readonly decided = computed(() => this.score().a >= this.winsNeeded() || this.score().b >= this.winsNeeded());
  readonly ready = computed(() => this.editable() && this.decided());
  readonly draftLabel = computed(() => {
    const draft = this.draft();
    if (this.confirmed() || !draft) return null;
    if (draft.by) return `draft · ${draft.by}`;
    // Own draft waiting for Confirm: the button says it all
    return this.ready() ? null : 'draft';
  });

  readonly teamA = computed(() => this.side(this.match().teamAId));
  readonly teamB = computed(() => this.side(this.match().teamBId));

  change(side: Side, delta: 1 | -1): void {
    if (!this.editable() || this.busy()) return;
    const { a, b } = this.score();
    const next = side === 'a' ? { a: a + delta, b } : { a, b: b + delta };
    if (next.a < 0 || next.b < 0) return;
    if (delta === 1 && this.decided()) return;
    this.draftChange.emit({ match: this.match(), teamAWins: next.a, teamBWins: next.b });
  }

  private side(teamId: number | null) {
    const known = !!teamId;
    const winnerId = this.match().winnerId;
    return {
      name: known ? (this.teamNames().get(teamId!) ?? 'Unknown team') : 'TBD',
      known,
      won: known && winnerId === teamId,
      lost: known && winnerId !== null && winnerId !== teamId,
    };
  }
}
