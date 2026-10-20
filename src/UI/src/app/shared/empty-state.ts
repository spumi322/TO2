import { Component, input } from '@angular/core';

// Placeholder for an empty list or a stage that has not started; actions go in the content slot
@Component({
  selector: 'app-empty-state',
  template: `
    <div class="empty">
      <i [class]="'pi ' + icon()" aria-hidden="true"></i>
      <h3>{{ title() }}</h3>
      @if (message()) {
        <p class="muted">{{ message() }}</p>
      }
      <ng-content />
    </div>
  `,
  styles: `
    .empty {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 8px;
      text-align: center;
      padding: 40px 16px;
      border: 1px dashed var(--border);
      border-radius: var(--radius);
    }
    i {
      font-size: 28px;
      color: var(--text-muted);
    }
    p {
      margin: 0;
      max-width: 420px;
    }
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input<string>();
  readonly icon = input('pi-inbox');
}
