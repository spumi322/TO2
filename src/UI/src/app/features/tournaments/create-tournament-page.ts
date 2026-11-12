import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Textarea } from 'primeng/textarea';
import { TournamentsApi } from '../../core/api/tournaments.api';
import { applyFieldErrors, toApiError } from '../../core/http/api-error';
import { Format, FORMAT_DESCRIPTIONS, FORMAT_LABELS, formatHasBracket, formatHasGroups } from '../../shared/enums';
import { groupSeedingText } from '../../shared/format-text';
import {
  buildCreateRequest,
  DESCRIPTION_MAX,
  MAX_TEAMS,
  MIN_TEAMS,
  previewConfig,
  TournamentConfig,
  validateConfig,
} from './tournament-config';

@Component({
  selector: 'app-create-tournament-page',
  imports: [ReactiveFormsModule, RouterLink, Button, InputNumber, InputText, Textarea],
  templateUrl: './create-tournament-page.html',
  styleUrl: './create-tournament-page.css',
})
export class CreateTournamentPage {
  private readonly api = inject(TournamentsApi);
  private readonly router = inject(Router);

  protected readonly formats = [Format.GroupsAndBracket, Format.BracketOnly, Format.GroupsOnly].map((format) => ({
    value: format,
    label: FORMAT_LABELS[format],
    description: FORMAT_DESCRIPTIONS[format],
  }));
  protected readonly minTeams = MIN_TEAMS;
  protected readonly maxTeams = MAX_TEAMS;
  protected readonly descriptionMax = DESCRIPTION_MAX;
  protected readonly numberFields = ['maxTeams', 'numberOfGroups', 'advancingPerGroup'] as const;

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true }),
    description: new FormControl('', { nonNullable: true }),
    format: new FormControl(Format.GroupsAndBracket, { nonNullable: true }),
    maxTeams: new FormControl(8, { nonNullable: true }),
    numberOfGroups: new FormControl<number | null>(2),
    advancingPerGroup: new FormControl<number | null>(2),
  });

  private readonly value = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });
  private readonly config = computed<TournamentConfig>(() => ({ ...this.form.getRawValue(), ...this.value() }) as TournamentConfig);

  readonly hasGroups = computed(() => formatHasGroups(this.config().format));
  readonly hasBracket = computed(() => formatHasBracket(this.config().format));
  readonly isGroupsAndBracket = computed(() => this.config().format === Format.GroupsAndBracket);
  readonly preview = computed(() => previewConfig(this.config()));
  readonly seedingText = computed(() => {
    const config = this.config();
    return config.format === Format.GroupsAndBracket
      ? groupSeedingText(config.numberOfGroups ?? 0, config.advancingPerGroup ?? 0)
      : null;
  });
  readonly clientErrors = computed(() => validateConfig(this.config()));
  // The preview draws only a structure the server would accept
  readonly previewIssue = computed(() => {
    const errors = this.clientErrors();
    return errors.maxTeams ?? errors.numberOfGroups ?? errors.advancingPerGroup ?? null;
  });

  readonly submitted = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  selectFormat(format: Format): void {
    this.form.controls.format.setValue(format);
  }

  // Client check first (instant), then the server's own errors on the same fields
  fieldError(field: keyof TournamentConfig): string | null {
    const control = this.form.controls[field];
    const server = control.errors?.['server'] as string | undefined;
    if (server) return server;
    return this.submitted() || control.touched ? (this.clientErrors()[field] ?? null) : null;
  }

  submit(): void {
    this.submitted.set(true);
    if (Object.keys(this.clientErrors()).length > 0) return;

    this.submitting.set(true);
    this.error.set(null);
    this.api.create(buildCreateRequest(this.config())).subscribe({
      next: (created) => void this.router.navigate(['/tournaments', created.id, 'overview']),
      error: (error: unknown) => {
        this.submitting.set(false);
        const apiError = toApiError(error);
        const unmatched = apiError.kind === 'validation' ? applyFieldErrors(this.form, apiError) : [apiError.message];
        this.error.set(unmatched.length ? unmatched.join(' ') : null);
      },
    });
  }
}
