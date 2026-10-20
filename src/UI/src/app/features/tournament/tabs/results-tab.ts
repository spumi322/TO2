import { Component, inject } from '@angular/core';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-results-tab',
  template: `<p class="muted">Results</p>`,
})
export class ResultsTab {
  readonly store = inject(TournamentStore);
}
