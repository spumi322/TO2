import { Component, computed, input } from '@angular/core';
import { STATUS_INFO, TournamentStatus } from './enums';

@Component({
  selector: 'app-status-chip',
  template: `
    <span class="chip" [class.outline]="info().outline" [style.--chip-color]="'var(' + info().color + ')'">
      @if (info().live) {
        <span class="dot pulse" aria-hidden="true"></span>
      } @else if (info().seeding) {
        <i class="pi pi-spin pi-spinner" aria-hidden="true"></i>
      } @else if (status() === finished) {
        <i class="pi pi-trophy" aria-hidden="true"></i>
      } @else {
        <span class="dot" aria-hidden="true"></span>
      }
      {{ info().label }}
    </span>
  `,
  styles: `
    .chip {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 12px;
      font-weight: 600;
      white-space: nowrap;
      color: var(--chip-color);
      background: color-mix(in srgb, var(--chip-color) 12%, transparent);
    }
    .chip.outline {
      background: transparent;
      box-shadow: inset 0 0 0 1.5px var(--chip-color);
    }
    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: currentColor;
    }
    .pulse {
      animation: pulse 1.6s ease-in-out infinite;
    }
    i {
      font-size: 11px;
    }
    @keyframes pulse {
      50% {
        opacity: 0.3;
      }
    }
  `,
})
export class StatusChip {
  readonly status = input.required<TournamentStatus>();
  readonly info = computed(() => STATUS_INFO[this.status()]);
  protected readonly finished = TournamentStatus.Finished;
}
