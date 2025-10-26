import { Component, Input } from '@angular/core';
import { FinalStanding } from '../../../models/final-standing';

@Component({
  selector: 'app-top-results-card',
  templateUrl: './top-results-card.component.html',
  styleUrls: ['./top-results-card.component.css']
})
export class TopResultsCardComponent {
  @Input() finalStandings: FinalStanding[] = [];

  get topFourStandings(): FinalStanding[] {
    return this.finalStandings.slice(0, 4);
  }

  getRankClass(placement: number): string {
    if (placement === 1) return 'gold';
    if (placement === 2) return 'silver';
    if (placement === 3) return 'bronze';
    return '';
  }
}
