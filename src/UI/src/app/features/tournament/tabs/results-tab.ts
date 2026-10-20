import { Component, computed, inject } from '@angular/core';
import { EmptyState } from '../../../shared/empty-state';
import { ordinal, podiumTiers, toPlacementRows } from '../placements';
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
  readonly rows = computed(() => toPlacementRows(this.store.results(), this.totalRounds()));
  readonly podium = computed(() =>
    podiumTiers(this.store.results()).map((teams) => ({ label: ordinal(teams[0].placement), teams })),
  );
}
