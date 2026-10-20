import { Component, effect, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { Textarea } from 'primeng/textarea';

const REASON_MAX = 250;

// Confirmation for start/cancel/delete/remove actions, optionally with a reason (max 250)
@Component({
  selector: 'app-confirm-dialog',
  imports: [FormsModule, Button, Dialog, Textarea],
  template: `
    <p-dialog
      [header]="header()"
      [(visible)]="visible"
      [modal]="true"
      [draggable]="false"
      [closable]="!busy()"
      [style]="{ width: 'min(460px, calc(100vw - 32px))' }"
      appendTo="body"
    >
      <div class="stack">
        <p class="message">{{ message() }}</p>
        @if (withReason()) {
          <div class="field">
            <label for="confirm-reason">Reason <span class="muted">(optional)</span></label>
            <textarea pTextarea id="confirm-reason" rows="3" [maxlength]="reasonMax" [(ngModel)]="reason"></textarea>
            <span class="hint num">{{ reason().length }} / {{ reasonMax }}</span>
          </div>
        }
      </div>
      <ng-template #footer>
        <p-button label="Back" [text]="true" severity="secondary" [disabled]="busy()" (onClick)="visible.set(false)" />
        <p-button
          [label]="confirmLabel()"
          [severity]="danger() ? 'danger' : 'primary'"
          [loading]="busy()"
          (onClick)="confirmed.emit(reason().trim() || null)"
        />
      </ng-template>
    </p-dialog>
  `,
  styles: `
    .message {
      margin: 0;
      white-space: pre-line;
    }
    textarea {
      width: 100%;
    }
  `,
})
export class ConfirmDialog {
  readonly visible = model(false);
  readonly header = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Confirm');
  readonly danger = input(false);
  readonly withReason = input(false);
  readonly busy = input(false);
  readonly confirmed = output<string | null>();

  protected readonly reasonMax = REASON_MAX;
  protected readonly reason = signal('');

  constructor() {
    // Fresh reason each time the dialog opens
    effect(() => {
      if (this.visible()) this.reason.set('');
    });
  }
}
