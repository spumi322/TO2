import { Component, input } from '@angular/core';
import { Format } from './enums';

// Line icon per format: bracket, group grid, two groups into a bracket
@Component({
  selector: 'app-format-icon',
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2"
      stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
      @switch (format()) {
        @case (formats.BracketOnly) {
          <path d="M3 5h6v6h6" />
          <path d="M3 17h6v-6" />
          <path d="M15 11h6" />
        }
        @case (formats.GroupsOnly) {
          <rect x="3" y="3" width="7" height="7" rx="1.5" />
          <rect x="14" y="3" width="7" height="7" rx="1.5" />
          <rect x="3" y="14" width="7" height="7" rx="1.5" />
          <rect x="14" y="14" width="7" height="7" rx="1.5" />
        }
        @case (formats.GroupsAndBracket) {
          <rect x="3" y="3" width="7" height="7" rx="1.5" />
          <rect x="3" y="14" width="7" height="7" rx="1.5" />
          <path d="M10 6.5h4v11h-4" />
          <path d="M14 12h7" />
        }
      }
    </svg>
  `,
  styles: `
    :host {
      display: inline-flex;
    }
  `,
})
export class FormatIcon {
  readonly format = input.required<Format>();
  readonly size = input(15);
  protected readonly formats = Format;
}
