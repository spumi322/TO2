import { Component, inject } from '@angular/core';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-groups-tab',
  template: `<p class="muted">Groups</p>`,
})
export class GroupsTab {
  readonly store = inject(TournamentStore);
}
