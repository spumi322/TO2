import { Component, EventEmitter, Output } from '@angular/core';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  withReason?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <p-dialog [(visible)]="visible" [modal]="true" [showHeader]="false" [dismissableMask]="true"
              styleClass="app-confirm-dialog" [style]="{ width: '26rem' }">
      <div class="confirm-header">
        <i class="pi pi-exclamation-triangle"></i>
        <h3>{{ data.title }}</h3>
      </div>

      <div class="confirm-body">
        <p>{{ data.message }}</p>
        <div *ngIf="data.withReason" class="reason-field">
          <label for="confirm-reason">Reason <span>(optional)</span></label>
          <textarea id="confirm-reason" rows="3" maxlength="250" placeholder="e.g. venue unavailable"
                    [(ngModel)]="reason"></textarea>
        </div>
      </div>

      <div class="confirm-footer">
        <button type="button" class="btn-back" (click)="onCancel()">Go back</button>
        <button type="button" class="btn-danger" (click)="onConfirm()">{{ data.confirmLabel || 'Confirm' }}</button>
      </div>
    </p-dialog>
  `,
  styles: [`
    :host ::ng-deep .app-confirm-dialog {
      background: var(--bg-card);
      border: var(--border-width) solid var(--border-color);
      border-radius: var(--border-radius);
      box-shadow: var(--shadow-card);
      overflow: hidden;
    }

    :host ::ng-deep .app-confirm-dialog .p-dialog-content {
      padding: 0;
      background: var(--bg-card);
      color: var(--text-primary);
      border-radius: var(--border-radius);
    }

    .confirm-header {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem;
      background: var(--bg-dark);
      border-bottom: var(--border-width) solid var(--border-color);
    }

    .confirm-header i {
      color: var(--error-color);
      font-size: 1.1rem;
    }

    .confirm-header h3 {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
    }

    .confirm-body {
      padding: 1rem;
    }

    .confirm-body p {
      margin: 0;
      color: var(--text-muted);
      font-size: 0.9rem;
      line-height: 1.5;
    }

    .reason-field {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      margin-top: 1rem;
    }

    .reason-field label {
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
      color: var(--text-primary);
    }

    .reason-field label span {
      color: var(--text-disabled);
      text-transform: none;
      font-weight: 400;
    }

    .reason-field textarea {
      background: var(--bg-dark);
      color: var(--text-primary);
      border: var(--border-width) solid var(--border-color);
      border-radius: var(--border-radius);
      padding: 9px 12px;
      font-family: inherit;
      font-size: 0.9rem;
      resize: vertical;
    }

    .reason-field textarea::placeholder {
      color: var(--text-disabled);
      font-style: italic;
    }

    .reason-field textarea:focus {
      border-color: var(--primary-color);
      outline: none;
    }

    .confirm-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      border-top: var(--border-width) solid var(--border-color);
    }

    .confirm-footer button {
      padding: 8px 16px;
      border-radius: var(--border-radius);
      font-family: inherit;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: border-color 0.15s, color 0.15s, background 0.15s;
    }

    .btn-back {
      background: transparent;
      border: var(--border-width) solid var(--border-color);
      color: var(--text-muted);
    }

    .btn-back:hover {
      border-color: var(--text-muted);
      color: var(--text-primary);
    }

    .btn-danger {
      background: rgba(244, 67, 54, 0.12);
      border: var(--border-width) solid var(--error-color);
      color: var(--error-color);
    }

    .btn-danger:hover {
      background: var(--error-color);
      color: #ffffff;
    }
  `]
})
export class ConfirmDialogComponent {
  // Emits the trimmed reason (or undefined) when the user confirms
  @Output() confirmed = new EventEmitter<string | undefined>();

  visible = false;
  data: ConfirmDialogData = { title: '', message: '' };
  reason = '';

  open(data: ConfirmDialogData): void {
    this.data = data;
    this.reason = '';
    this.visible = true;
  }

  onConfirm(): void {
    this.visible = false;
    this.confirmed.emit(this.reason.trim() || undefined);
  }

  onCancel(): void {
    this.visible = false;
  }
}
