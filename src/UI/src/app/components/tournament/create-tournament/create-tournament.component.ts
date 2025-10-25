import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Router } from '@angular/router';
import { finalize, debounceTime, switchMap, map } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css']
})
export class CreateTournamentComponent implements OnInit {
  form!: FormGroup;
  Format = Format;
  isSubmitting = false;
  successMessage = '';
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private tournamentService: TournamentService,
    private router: Router
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

  onFormatChange(): void {
    const format = this.form.get('format')?.value;

    if (format === Format.BracketOnly) {
      this.form.get('maxTeams')?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(32)
      ]);
      this.form.get('teamsPerGroup')?.clearValidators();
      this.form.get('teamsPerBracket')?.clearValidators();

      // For BracketOnly format, maxTeams must equal teamsPerBracket
      this.form.get('teamsPerBracket')?.setValue(this.form.get('maxTeams')?.value);
    } else if (format === Format.BracketAndGroups) {
      this.form.get('teamsPerGroup')?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(16)
      ]);
      this.form.get('teamsPerBracket')?.setValidators([
        Validators.required,
        Validators.min(4),
        Validators.max(32)
      ]);
      this.form.get('maxTeams')?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(64),
        this.divisibleByValidator()
      ]);

      // Auto-calculate maxTeams if possible
      this.updateMaxTeams();
    }

    this.form.get('maxTeams')?.updateValueAndValidity();
    this.form.get('teamsPerGroup')?.updateValueAndValidity();
    this.form.get('teamsPerBracket')?.updateValueAndValidity();
  }

  // For BracketAndGroup format, maxTeams must be divisible by teamsPerGroup
  divisibleByValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const maxTeams = control.value;
      const teamsPerGroup = this.form.get('teamsPerGroup')?.value;

      if (!maxTeams || !teamsPerGroup || maxTeams % teamsPerGroup !== 0) {
        return { divisibleBy: true };
      }

      return null;
    };
  }

  // Auto-calculate total teams based on teams per group
  updateMaxTeams(): void {
    const teamsPerGroup = this.form.get('teamsPerGroup')?.value;
    const groupCount = 4; // Default number of groups (can be adjusted as needed)

    if (teamsPerGroup && !isNaN(teamsPerGroup)) {
      const maxTeams = teamsPerGroup * groupCount;
      this.form.get('maxTeams')?.setValue(maxTeams);
    }
  }

  // Handle form submission
  submit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const newTournament: Tournament = this.form.value;

    this.tournamentService.createTournament(newTournament)
      .pipe(
        finalize(() => this.isSubmitting = false)
      )
      .subscribe({
        next: (res) => {
          this.successMessage = 'Tournament created successfully!';
          setTimeout(() => {
            this.router.navigate(['/tournament', res.id]);
          }, 1500);
        },
        error: (err) => {
          console.error('Error creating tournament:', err);
          this.errorMessage = err.error?.message || 'Failed to create tournament. Please try again.';
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
}
