import { Component, Input } from '@angular/core';
import { FinalStanding } from '../../../models/final-standing';

interface PlacementTier {
  label: string;
  teams: FinalStanding[];
}

@Component({
  selector: 'app-final-standings-display',
  templateUrl: './final-standings-display.component.html',
  styleUrls: ['./final-standings-display.component.css']
})
export class FinalStandingsDisplayComponent {
  @Input() finalStandings: FinalStanding[] = [];

  groupPlacementsByTier(): PlacementTier[] {
    if (!this.finalStandings || this.finalStandings.length === 0) {
      return [];
    }

    const tiers: PlacementTier[] = [];
    const grouped = new Map<string, FinalStanding[]>();

    // Group by placement value
    this.finalStandings.forEach(standing => {
      let tierLabel = '';
      const p = standing.placement;

      if (p === 1) {
        tierLabel = '1st Place';
      } else if (p === 2) {
        tierLabel = '2nd Place';
      } else if (p >= 3 && p <= 4) {
        tierLabel = '3rd-4th Place';
      } else if (p >= 5 && p <= 8) {
        tierLabel = '5th-8th Place';
      } else if (p >= 9 && p <= 16) {
        tierLabel = '9th-16th Place';
      } else if (p >= 17 && p <= 32) {
        tierLabel = '17th-32nd Place';
      } else {
        tierLabel = `${p}th Place`;
      }

      if (!grouped.has(tierLabel)) {
        grouped.set(tierLabel, []);
      }
      grouped.get(tierLabel)!.push(standing);
    });

    // Convert to array
    grouped.forEach((teams, label) => {
      tiers.push({ label, teams });
    });

    return tiers;
  }
}
