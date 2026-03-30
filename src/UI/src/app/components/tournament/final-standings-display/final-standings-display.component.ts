import { Component, Input } from '@angular/core';
import { FinalStanding } from '../../../models/final-standing';

@Component({
  selector: 'app-final-standings-display',
  templateUrl: './final-standings-display.component.html',
  styleUrls: ['./final-standings-display.component.css']
})
export class FinalStandingsDisplayComponent {
  @Input() finalStandings: FinalStanding[] = [];

  get champion(): FinalStanding | null {
    return this.finalStandings.find(s => s.placement === 1) ?? null;
  }

  get podiumStandings(): FinalStanding[] {
    return this.finalStandings
      .filter(s => s.placement >= 2 && s.placement <= 4)
      .sort((a, b) => a.placement - b.placement);
  }

  get lowerTiers(): { label: string; standings: FinalStanding[] }[] {
    const tiers = [
      { label: 'Top 8', min: 5, max: 8 },
      { label: 'Top 16', min: 9, max: 16 },
      { label: 'Top 32', min: 17, max: 32 },
    ];
    return tiers
      .map(t => ({
        label: t.label,
        standings: this.finalStandings
          .filter(s => s.placement >= t.min && s.placement <= t.max)
          .sort((a, b) => a.placement - b.placement)
      }))
      .filter(t => t.standings.length > 0);
  }

  ordinal(n: number): string {
    if (n === 2) return '2nd';
    if (n === 3) return '3rd';
    if (n === 4) return '4th';
    return `${n}th`;
  }
}
