import { Component, inject } from '@angular/core';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-teams-tab',
  template: `<p class="muted">Teams</p>`,
})
export class TeamsTab {
  readonly store = inject(TournamentStore);
}
