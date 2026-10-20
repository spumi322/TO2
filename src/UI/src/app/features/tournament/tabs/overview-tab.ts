import { Component, inject } from '@angular/core';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-overview-tab',
  template: `<p class="muted">Overview</p>`,
})
export class OverviewTab {
  readonly store = inject(TournamentStore);
}
