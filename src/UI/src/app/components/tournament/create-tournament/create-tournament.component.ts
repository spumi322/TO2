import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css']
})
export class CreateTournamentComponent implements OnInit {
  form!: FormGroup;
  Format = Format;
  isSubmitting = false;

  bracketSizeOptions = [
    { label: '4 Teams', value: 4 },
    { label: '8 Teams', value: 8 },
    { label: '16 Teams', value: 16 },
    { label: '32 Teams', value: 32 }
  ];

  constructor(
    private fb: FormBuilder,
    private tournamentService: TournamentService,
    private router: Router,
    private messageService: MessageService
  ) { }

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm(): void {
    this.form = this.fb.group({
      name: ['', [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(100)
      ]],
      description: ['', [
        Validators.maxLength(250)
      ]],
      format: [null, Validators.required],
      maxTeams: [''],
      teamsPerGroup: [''],
      teamsPerBracket: ['']
    });
  }

  selectFormat(format: Format): void {
    this.form.patchValue({ format });
    this.onFormatChange();
  }

onFormatChange(): void {
    const format = this.form.get('format')?.value;

    // Clear all fields
    this.form.get('maxTeams')?.setValue('');
    this.form.get('teamsPerGroup')?.setValue('');
    this.form.get('teamsPerBracket')?.setValue('');

    if (format === Format.BracketOnly) {
        // BracketOnly: Power of 2 validation, no groups
        this.form.get('maxTeams')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32),
            this.powerOfTwoValidator()
        ]);

        this.form.get('teamsPerGroup')?.clearValidators();
        this.form.get('teamsPerBracket')?.clearValidators();
        this.form.get('teamsPerGroup')?.setValue(null);
        this.form.get('teamsPerBracket')?.setValue(null);

    } else if (format === Format.GroupsAndBracket) {
        // GroupsAndBracket: both groups and bracket required
        this.form.get('teamsPerGroup')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32)
        ]);

        this.form.get('teamsPerBracket')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32),
            this.powerOfTwoValidator()
        ]);

        this.form.get('maxTeams')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32),
            this.divisibleByValidator()
        ]);

    } else if (format === Format.GroupsOnly) {
        // GroupsOnly: only groups, no bracket
        this.form.get('teamsPerGroup')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32)
        ]);

        this.form.get('teamsPerBracket')?.clearValidators();
        this.form.get('teamsPerBracket')?.setValue(null);

        this.form.get('maxTeams')?.setValidators([
            Validators.required,
            Validators.min(4),
            Validators.max(32),
            this.divisibleByValidator()
        ]);
    }

    this.form.get('maxTeams')?.updateValueAndValidity();
    this.form.get('teamsPerGroup')?.updateValueAndValidity();
    this.form.get('teamsPerBracket')?.updateValueAndValidity();
}

  // For BracketOnly format, maxTeams must be power of 2
  powerOfTwoValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;

      const isPowerOfTwo = value > 0 && (value & (value - 1)) === 0;
      return isPowerOfTwo ? null : { powerOfTwo: true };
    };
  }

  // For GroupsAndBracket and GroupsOnly formats, maxTeams must be divisible by teamsPerGroup
  divisibleByValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const maxTeams = control.value;
      const teamsPerGroup = this.form.get('teamsPerGroup')?.value;

      if (!maxTeams || !teamsPerGroup) return null;

      if (maxTeams % teamsPerGroup !== 0) {
        return { divisibleBy: true };
      }

      return null;
    };
  }

  // Handle form submission
  submit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation Error',
        detail: 'Please fix all validation errors before submitting'
      });
      return;
    }

    this.isSubmitting = true;

    const tournament: Tournament = { ...this.form.value };

    // For BracketOnly, teamsPerBracket should equal maxTeams
    if (tournament.format === Format.BracketOnly) {
      tournament.teamsPerBracket = tournament.maxTeams;
    }

    this.tournamentService.createTournament(tournament)
      .pipe(finalize(() => this.isSubmitting = false))
      .subscribe({
        next: (res) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Tournament created successfully!'
          });
          setTimeout(() => {
            this.router.navigate(['/tournament', res.id]);
          }, 1500);
        },
        error: (err) => {
          console.error('Error creating tournament:', err);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: err.error?.message || 'Failed to create tournament. Please try again.'
          });
        }
      });
  }

  // Utility to mark all form controls as touched to trigger validation messages
  markFormGroupTouched(formGroup: FormGroup): void {
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }

  // Get error message for tooltip display
  getErrorMessage(fieldName: string): string | undefined {
    const control = this.form.get(fieldName);

    if (!control || !control.invalid || !control.touched) {
      return undefined;
    }

    let message: string | undefined;

    // Name field errors
    if (fieldName === 'name') {
      if (control.hasError('required')) message = 'Tournament name is required';
      else if (control.hasError('minlength')) message = 'Name must be at least 4 characters';
      else if (control.hasError('maxlength')) message = 'Name cannot exceed 100 characters';
    }

    // Description field errors
    else if (fieldName === 'description') {
      if (control.hasError('maxlength')) message = 'Description cannot exceed 250 characters';
    }

    // Format field errors
    else if (fieldName === 'format') {
      if (control.hasError('required')) message = 'Please select a tournament format';
    }

    // MaxTeams field errors (Bracket)
    else if (fieldName === 'maxTeams') {
      if (control.hasError('required')) message = 'Tournament size is required';
      else if (control.hasError('powerOfTwo')) message = 'Must be power of 2 (2, 4, 8, 16, 32)';
      else if (control.hasError('divisibleBy')) message = 'Must be divisible by group size';
    }

    // TeamsPerGroup field errors
    else if (fieldName === 'teamsPerGroup') {
      if (control.hasError('required')) message = 'Required';
      else if (control.hasError('min') || control.hasError('max')) message = 'Must be 2-16';
    }

    // TeamsPerBracket field errors
    else if (fieldName === 'teamsPerBracket') {
      if (control.hasError('required')) message = 'Required';
    }

    console.log(`Tooltip for ${fieldName}: ${message}`);
    return message;
  }
}
