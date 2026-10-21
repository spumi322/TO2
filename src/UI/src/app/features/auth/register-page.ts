import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { AuthService } from '../../core/auth/auth.service';
import { applyFieldErrors, toApiError } from '../../core/http/api-error';

// Mirrors the Identity password options in Program.cs (the server still decides)
const passwordRules: ValidatorFn = (control) => {
  const value = String(control.value ?? '');
  const ok = value.length >= 6 && /\d/.test(value) && /[a-z]/.test(value) && /[A-Z]/.test(value);
  return ok ? null : { passwordRules: true };
};

type OrgState = 'unknown' | 'checking' | 'new' | 'exists' | 'checkFailed';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, Button, InputText],
  templateUrl: './register-page.html',
  styleUrl: './auth-page.css',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly inviteInput = viewChild<ElementRef<HTMLInputElement>>('inviteInput');

  readonly form = new FormGroup({
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [passwordRules] }),
    tenantName: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
    inviteCode: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(50)] }),
  });

  readonly orgState = signal<OrgState>('unknown');
  readonly orgCheckMessage = signal<string | null>(null);
  // Shown when the organization exists (check on blur, or a 409 on submit)
  readonly showInvite = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  private checkedName = '';

  // On blur, not per keystroke: the check shares the 10/min auth rate limit with login and register
  checkOrganization(): void {
    const name = this.form.controls.tenantName.value.trim();
    if (!name || name.toLowerCase() === this.checkedName.toLowerCase()) return;

    this.checkedName = name;
    this.orgState.set('checking');
    this.orgCheckMessage.set(null);
    this.auth.organizationExists(name).subscribe({
      next: (exists) => {
        this.orgState.set(exists ? 'exists' : 'new');
        this.setInviteShown(exists);
      },
      error: (error: unknown) => {
        this.checkedName = '';
        this.orgState.set('checkFailed');
        this.orgCheckMessage.set(toApiError(error).message);
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const inviteCode = this.showInvite() && value.inviteCode.trim() ? value.inviteCode.trim() : undefined;

    this.submitting.set(true);
    this.error.set(null);
    this.auth.register({ ...value, tenantName: value.tenantName.trim(), inviteCode }).subscribe({
      next: () => void this.router.navigateByUrl('/tournaments'),
      error: (error: unknown) => {
        this.submitting.set(false);
        this.handleError(error);
      },
    });
  }

  // A shown invite field is required: joining an existing organization needs a code
  private setInviteShown(shown: boolean): void {
    this.showInvite.set(shown);
    const inviteCode = this.form.controls.inviteCode;
    inviteCode.setValidators(shown ? [Validators.required, Validators.maxLength(50)] : [Validators.maxLength(50)]);
    inviteCode.updateValueAndValidity();
  }

  private handleError(error: unknown): void {
    const apiError = toApiError(error);
    const message = apiError.message;

    if (apiError.kind === 'validation') {
      const unmatched = applyFieldErrors(this.form, apiError);
      this.error.set(unmatched.length ? unmatched.join(' ') : null);
    } else if (apiError.kind === 'conflict' && /invite code/i.test(message)) {
      // The organization exists (or was just created by someone else): joining needs a code
      this.setInviteShown(true);
      this.orgState.set('exists');
      this.error.set(message);
      setTimeout(() => this.inviteInput()?.nativeElement.focus());
    } else if (apiError.kind === 'conflict' && /email/i.test(message)) {
      this.form.controls.email.setErrors({ server: message });
    } else if (apiError.kind === 'badRequest' && /invite code/i.test(message) && this.showInvite()) {
      this.form.controls.inviteCode.setErrors({ server: message });
    } else {
      this.error.set(message);
    }
  }
}
