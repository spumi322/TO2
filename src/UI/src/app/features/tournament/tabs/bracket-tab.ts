import { Component, inject } from '@angular/core';
import { TournamentStore } from '../tournament.store';

@Component({
  selector: 'app-bracket-tab',
  template: `<p class="muted">Bracket</p>`,
})
export class BracketTab {
  readonly store = inject(TournamentStore);
}
