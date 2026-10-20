import { Component, computed, input } from '@angular/core';

const PALETTE = ['#00d936', '#00bcd4', '#9c27b0', '#ff6f00', '#e91e8c', '#1565c0', '#c62828', '#00796b'];

// Shield with the team's initials; the color is derived from the name, so a team keeps its color everywhere
@Component({
  selector: 'app-team-badge',
  template: `
    <span class="shield" [class]="size()" [style.background]="color()" aria-hidden="true">
      <span class="initials">{{ initials() }}</span>
    </span>
    <span class="name" [class]="size()">{{ teamName() }}</span>
  `,
  styles: `
    :host {
      display: inline-flex;
      align-items: center;
      gap: 10px;
      min-width: 0;
    }
    .shield {
      width: 40px;
      height: 46px;
      clip-path: polygon(0% 0%, 100% 0%, 100% 68%, 50% 100%, 0% 68%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding-bottom: 8px;
      flex-shrink: 0;
    }
    .shield.lg {
      width: 56px;
      height: 64px;
      padding-bottom: 12px;
    }
    .shield.sm {
      width: 30px;
      height: 34px;
      padding-bottom: 6px;
    }
    .shield.xs {
      width: 22px;
      height: 25px;
      padding-bottom: 4px;
    }
    .initials {
      font-weight: 800;
      font-size: 13px;
      color: #0e1117;
      letter-spacing: 0.03em;
      line-height: 1;
    }
    .lg .initials {
      font-size: 17px;
    }
    .sm .initials {
      font-size: 10px;
    }
    .xs .initials {
      font-size: 8px;
    }
    .name {
      font-weight: 600;
      font-size: 16px;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .name.lg {
      font-size: 20px;
      font-weight: 700;
    }
    .name.sm {
      font-size: 14px;
    }
    .name.xs {
      font-size: 13px;
      font-weight: 500;
    }
  `,
})
export class TeamBadge {
  readonly teamName = input.required<string>();
  readonly size = input<'xs' | 'sm' | 'md' | 'lg'>('md');

  readonly initials = computed(() => {
    const words = this.teamName().trim().split(/\s+/);
    if (words.length === 1) return words[0].charAt(0).toUpperCase();
    return (words[0].charAt(0) + words[1].charAt(0)).toUpperCase();
  });

  readonly color = computed(() => {
    const name = this.teamName();
    let hash = 0;
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return PALETTE[Math.abs(hash) % PALETTE.length];
  });
}
