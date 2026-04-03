import { Component, Input } from '@angular/core';

const PALETTE = [
  '#00d936', // green
  '#00bcd4', // cyan
  '#9c27b0', // purple
  '#ff6f00', // orange
  '#e91e8c', // pink
  '#1565c0', // blue
  '#c62828', // red
  '#00796b', // teal
];

@Component({
  selector: 'app-team-badge',
  templateUrl: './team-badge.component.html',
  styleUrls: ['./team-badge.component.css']
})
export class TeamBadgeComponent {
  @Input() teamName: string = '';
  @Input() size: 'xs' | 'sm' | 'md' | 'lg' = 'md';

  get initials(): string {
    const words = this.teamName.trim().split(/\s+/);
    if (words.length === 1) return words[0].charAt(0).toUpperCase();
    return (words[0].charAt(0) + words[1].charAt(0)).toUpperCase();
  }

  get shieldColor(): string {
    let hash = 0;
    for (let i = 0; i < this.teamName.length; i++) {
      hash = this.teamName.charCodeAt(i) + ((hash << 5) - hash);
    }
    return PALETTE[Math.abs(hash) % PALETTE.length];
  }

  get shieldStyle(): { background: string; filter: string } {
    const color = this.shieldColor;
    return {
      background: color,
      filter: `drop-shadow(0 0 5px ${color}80)`
    };
  }
}
