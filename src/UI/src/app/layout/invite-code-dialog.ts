import { DatePipe } from '@angular/common';
import { Component, inject, model, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InviteCodeResponse } from '../core/auth/auth.models';
import { AuthService } from '../core/auth/auth.service';
import { SessionStore } from '../core/auth/session.store';
import { toApiError } from '../core/http/api-error';

// The server stores only a hash, so a code is shown once; generating a new one replaces the old
@Component({
  selector: 'app-invite-code-dialog',
  imports: [DatePipe, Button, Dialog],
  templateUrl: './invite-code-dialog.html',
  styleUrl: './invite-code-dialog.css',
})
export class InviteCodeDialog {
  private readonly auth = inject(AuthService);
  readonly session = inject(SessionStore);

  readonly visible = model(false);
  readonly invite = signal<InviteCodeResponse | null>(null);
  readonly generating = signal(false);
  readonly error = signal<string | null>(null);
  readonly copied = signal(false);

  generate(): void {
    this.generating.set(true);
    this.error.set(null);
    this.auth.generateInviteCode().subscribe({
      next: (invite) => {
        this.invite.set(invite);
        this.generating.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiError(error).message);
        this.generating.set(false);
      },
    });
  }

  async copy(code: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(code);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch {
      this.error.set('Copy failed: select the code and copy it by hand.');
    }
  }

  reset(): void {
    this.invite.set(null);
    this.error.set(null);
    this.copied.set(false);
  }
}
