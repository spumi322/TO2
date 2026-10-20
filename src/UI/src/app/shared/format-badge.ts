import { Component, computed, input } from '@angular/core';
import { Format, FORMAT_LABELS } from './enums';

@Component({
  selector: 'app-format-badge',
  template: `<span class="badge">{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px 8px;
      border-radius: var(--radius-sm);
      border: 1px solid var(--border);
      color: var(--text-muted);
      font-size: 12px;
      font-weight: 500;
      white-space: nowrap;
    }
  `,
})
export class FormatBadge {
  readonly format = input.required<Format>();
  readonly label = computed(() => FORMAT_LABELS[this.format()]);
}
