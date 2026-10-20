import { Component, computed, inject } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state';
import { Format } from '../../../shared/enums';import { ordinal, podiumTiers, toPlacementRows } from '../placements';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-results-tab',
  imports: [EmptyState],
  templateUrl: './results-tab.html',
  styleUrl: './results-tab.css',
})
export class ResultsTab {
  readonly store = inject(TournamentStore);

  private readonly totalRounds = computed(() =>
    Math.max(0, ...(this.store.bracket()?.matches ?? []).map((match) => match.round ?? 0)),
  );
  readonly rows = computed(() =>
    toPlacementRows(this.store.results(), this.totalRounds(), this.store.tournament()?.format === Format.GroupsOnly),
  );

  // Long tables are split into side-by-side columns (at most 12 rows each), so 32 teams fit one screen
  readonly columns = computed(() => {
    const rows = this.rows();
    const count = Math.min(3, Math.ceil(rows.length / 12));
    const size = Math.ceil(rows.length / Math.max(1, count));
    return Array.from({ length: count }, (_, i) => rows.slice(i * size, (i + 1) * size));
  });
  readonly podium = computed(() =>
    podiumTiers(this.store.results()).map((teams) => ({ label: ordinal(teams[0].placement), teams })),
  );
}
