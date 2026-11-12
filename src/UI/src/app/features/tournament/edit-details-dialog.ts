import { Component, computed, inject, model, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { applyFieldErrors, toApiError } from '../../core/http/api-error';
import { DESCRIPTION_MAX, validateDetails } from '../tournaments/tournament-config';
import { TournamentStore } from './tournament.store';

type Field = 'name' | 'description';

/**
 * Name and description of a tournament that has not started, with the create form's fields and rules.
 * Created each time it opens, so it starts from the tournament as it is now.
 */
@Component({
  selector: 'app-edit-details-dialog',
  imports: [ReactiveFormsModule, Button, Dialog, InputText, Textarea],
  template: `
    <p-dialog
      header="Edit tournament"
      [(visible)]="visible"
      [modal]="true"
      [draggable]="false"
      [closable]="!saving()"
      [style]="{ width: 'min(460px, calc(100vw - 32px))' }"
      appendTo="body"
    >
      <form class="stack" [formGroup]="form" (ngSubmit)="save()" novalidate>
        @if (error(); as message) {
          <div class="form-error" role="alert">{{ message }}</div>
        }

        <div class="field">
          <label for="edit-name">Name</label>
          <input pInputText id="edit-name" formControlName="name" maxlength="60" autocomplete="off" />
          @if (fieldError('name'); as message) {
            <span class="field-error">{{ message }}</span>
          }
        </div>

        <div class="field">
          <label for="edit-description">Description</label>
          <textarea pTextarea id="edit-description" formControlName="description" rows="3" [maxlength]="descriptionMax"
            placeholder="Dates, prizes, rules..."></textarea>
          <span class="hint num">{{ form.controls.description.value.length }} / {{ descriptionMax }}</span>
          @if (fieldError('description'); as message) {
            <span class="field-error">{{ message }}</span>
          }
        </div>
      </form>
      <ng-template #footer>
        <p-button label="Back" [text]="true" severity="secondary" [disabled]="saving()" (onClick)="visible.set(false)" />
        <p-button label="Save" [loading]="saving()" (onClick)="save()" />
      </ng-template>
    </p-dialog>
  `,
  styles: `
    input,
    textarea {
      width: 100%;
    }
  `,
})
export class EditDetailsDialog {
  private readonly store = inject(TournamentStore);
  private readonly messages = inject(MessageService);

  readonly visible = model(false);

  protected readonly descriptionMax = DESCRIPTION_MAX;
  private readonly original = this.store.tournament()!;
  // Sent with the save: a change made by someone else while the form was open is a conflict, not overwritten
  private version = this.original.version;

  readonly form = new FormGroup({
    name: new FormControl(this.original.name, { nonNullable: true }),
    description: new FormControl(this.original.description ?? '', { nonNullable: true }),
  });

  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  private readonly clientErrors = computed(() => validateDetails({ ...this.form.getRawValue(), ...this.value() }));

  readonly submitted = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  fieldError(field: Field): string | null {
    const control = this.form.controls[field];
    const server = control.errors?.['server'] as string | undefined;
    if (server) return server;
    return this.submitted() || control.touched ? (this.clientErrors()[field] ?? null) : null;
  }

  async save(): Promise<void> {
    this.submitted.set(true);
    if (Object.keys(this.clientErrors()).length > 0 || this.saving()) return;

    const name = this.form.controls.name.value.trim();
    const description = this.form.controls.description.value.trim() || null;
    if (name === this.original.name && description === this.original.description) {
      this.visible.set(false);
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      await this.store.updateDetails(name, description, this.version);
      this.messages.add({ severity: 'success', summary: 'Tournament updated' });
      this.visible.set(false);
    } catch (error) {
      const apiError = toApiError(error);
      const unmatched = apiError.kind === 'validation' ? applyFieldErrors(this.form, apiError) : [apiError.message];
      this.error.set(unmatched.length ? unmatched.join(' ') : null);
      // The store reloaded on a conflict: a second Save goes against what is there now
      this.version = this.store.tournament()?.version ?? this.version;
    } finally {
      this.saving.set(false);
    }
  }
}
