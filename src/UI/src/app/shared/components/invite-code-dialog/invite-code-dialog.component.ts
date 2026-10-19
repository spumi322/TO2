import { Component } from '@angular/core';
import { AuthService } from '../../../services/auth/auth.service';
import { InviteCodeResponse } from '../../../models/auth/invite-code-response.model';

@Component({
  selector: 'app-invite-code-dialog',
  template: `
    <p-dialog [(visible)]="visible" [modal]="true" [showHeader]="false" [dismissableMask]="true" appendTo="body"
              styleClass="app-invite-dialog" [style]="{ width: '28rem' }">
      <div class="invite-header">
        <i class="pi pi-user-plus"></i>
        <h3>Invite to {{ organizationName }}</h3>
      </div>

      <div class="invite-body">
        <p *ngIf="!invite">
          Generate a code and share it with the person joining. They enter it on the register page
          with this organization's name. The code works for anyone for 1 day.
          Generating a new code replaces the current one.
        </p>

        <ng-container *ngIf="invite">
          <div class="invite-code" data-testid="invite-code">{{ invite.code }}</div>
          <p>Valid until {{ invite.expiresAt | date:'medium' }}. Any older code no longer works.</p>
        </ng-container>

        <p *ngIf="errorMessage" class="invite-error">{{ errorMessage }}</p>
      </div>

      <div class="invite-footer">
        <button type="button" class="btn-back" (click)="visible = false">Close</button>
        <button *ngIf="invite" type="button" class="btn-secondary" (click)="copy()">{{ copied ? 'Copied' : 'Copy' }}</button>
        <button type="button" class="btn-primary" [disabled]="isLoading" (click)="generate()">
          {{ invite ? 'Generate new code' : 'Generate code' }}
        </button>
      </div>
    </p-dialog>
  `,
  styles: [`
    /* The dialog is appended to <body> (outside the navbar), so these two rules cannot use :host */
    ::ng-deep .app-invite-dialog {
      background: var(--bg-card);
      border: var(--border-width) solid var(--border-color);
      border-radius: var(--border-radius);
      box-shadow: var(--shadow-card);
      overflow: hidden;
    }

    ::ng-deep .app-invite-dialog .p-dialog-content {
      padding: 0;
      background: var(--bg-card);
      color: var(--text-primary);
      border-radius: var(--border-radius);
    }

    .invite-header {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem;
      background: var(--bg-dark);
      border-bottom: var(--border-width) solid var(--border-color);
    }

    .invite-header i {
      color: var(--primary-color);
      font-size: 1.1rem;
    }

    .invite-header h3 {
      margin: 0;
      font-size: 1rem;
      font-weight: 600;
    }

    .invite-body {
      padding: 1rem;
    }

    .invite-body p {
      margin: 0;
      color: var(--text-muted);
      font-size: 0.9rem;
      line-height: 1.5;
    }

    .invite-code {
      margin-bottom: 0.75rem;
      padding: 0.75rem;
      text-align: center;
      font-family: monospace;
      font-size: 1.1rem;
      letter-spacing: 0.05em;
      background: var(--bg-dark);
      border: var(--border-width) solid var(--border-color);
      border-radius: var(--border-radius);
      user-select: all;
    }

    .invite-body .invite-error {
      margin-top: 0.75rem;
      color: var(--error-color);
    }

    .invite-footer {
      display: flex;
      justify-content: flex-end;
      gap: 0.5rem;
      padding: 0.75rem 1rem;
      border-top: var(--border-width) solid var(--border-color);
    }

    .invite-footer button {
      padding: 8px 16px;
      border-radius: var(--border-radius);
      font-family: inherit;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      background: transparent;
      transition: border-color 0.15s, color 0.15s, background 0.15s;
    }

    .btn-back,
    .btn-secondary {
      border: var(--border-width) solid var(--border-color);
      color: var(--text-muted);
    }

    .btn-back:hover,
    .btn-secondary:hover {
      border-color: var(--text-muted);
      color: var(--text-primary);
    }

    .btn-primary {
      border: var(--border-width) solid var(--primary-color);
      color: var(--primary-color);
    }

    .btn-primary:hover {
      background: var(--primary-color);
      color: #ffffff;
    }
  `]
})
export class InviteCodeDialogComponent {
  visible = false;
  isLoading = false;
  copied = false;
  organizationName = '';
  invite: InviteCodeResponse | null = null;
  errorMessage = '';

  constructor(private authService: AuthService) {}

  open(organizationName: string): void {
    this.organizationName = organizationName;
    this.invite = null;
    this.errorMessage = '';
    this.copied = false;
    this.visible = true;
  }

  generate(): void {
    this.isLoading = true;
    this.errorMessage = '';
    this.authService.createInviteCode().subscribe({
      next: invite => {
        this.invite = invite;
        this.copied = false;
        this.isLoading = false;
      },
      error: error => {
        this.errorMessage = error.error?.detail || 'Could not generate an invite code.';
        this.isLoading = false;
      }
    });
  }

  copy(): void {
    if (!this.invite) return;
    navigator.clipboard.writeText(this.invite.code).then(() => this.copied = true);
  }
}
