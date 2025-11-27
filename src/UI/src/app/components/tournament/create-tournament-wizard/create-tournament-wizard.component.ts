import { Component, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatStepper } from '@angular/material/stepper';
import { StepperSelectionEvent } from '@angular/cdk/stepper';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';

import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-create-tournament-wizard',
  templateUrl: './create-tournament-wizard.component.html',
  styleUrls: ['./create-tournament-wizard.component.css']
})
export class CreateTournamentWizardComponent implements OnInit {
  @ViewChild('stepper') stepper!: MatStepper;

  // Stepper configuration
  isLinear = true;

  // Forms
  detailsForm!: FormGroup;
  formatForm!: FormGroup;
  configForm!: FormGroup;

  // State
  Format = Format;
  selectedFormat: Format | null = null;
  isSubmitting = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private tournamentService: TournamentService,
    private router: Router,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.initializeForms();
  }

  initializeForms(): void {
    // Step 1: Tournament Details
    this.detailsForm = this.fb.group({
      name: ['', [
        Validators.required,
        Validators.minLength(4),
        Validators.maxLength(100)
      ]],
      description: ['', [
        Validators.maxLength(250)
      ]]
    });

    // Step 2: Format Selection
    this.formatForm = this.fb.group({
      format: [null, Validators.required]
    });

    // Step 3: Team Configuration (dynamic validators)
    this.configForm = this.fb.group({
      maxTeams: [''],
      teamsPerGroup: [''],
      teamsPerBracket: ['']
    });

    // Watch format changes
    this.formatForm.get('format')?.valueChanges.subscribe(format => {
      this.onFormatChange(format);
    });
  }

  onFormatChange(format: Format): void {
    this.selectedFormat = format;
    this.resetConfigForm(format);
  }

  resetConfigForm(format: Format): void {
    // Clear all values and validators
    this.configForm.reset();

    const maxTeamsControl = this.configForm.get('maxTeams');
    const teamsPerGroupControl = this.configForm.get('teamsPerGroup');
    const teamsPerBracketControl = this.configForm.get('teamsPerBracket');

    // Apply format-specific validators
    if (format === Format.BracketOnly) {
      // BracketOnly: Power of 2 validation, no groups
      maxTeamsControl?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(32),
        this.powerOfTwoValidator()
      ]);
      teamsPerGroupControl?.clearValidators();
      teamsPerGroupControl?.setValue(null);
      teamsPerBracketControl?.clearValidators();
      teamsPerBracketControl?.setValue(null);
    } else if (format === Format.GroupsAndBracket) {
      // GroupsAndBracket: Groups followed by bracket
      teamsPerGroupControl?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(16)
      ]);
      teamsPerBracketControl?.setValidators([
        Validators.required,
        Validators.min(4),
        Validators.max(32)
      ]);
      maxTeamsControl?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(64),
        this.divisibleByValidator()
      ]);
    } else if (format === Format.GroupsOnly) {
      // GroupsOnly: Only groups, no bracket
      teamsPerGroupControl?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(16)
      ]);
      teamsPerBracketControl?.clearValidators();
      teamsPerBracketControl?.setValue(null);
      maxTeamsControl?.setValidators([
        Validators.required,
        Validators.min(2),
        Validators.max(32),
        this.divisibleByValidator()
      ]);
    }

    maxTeamsControl?.updateValueAndValidity();
    teamsPerGroupControl?.updateValueAndValidity();
    teamsPerBracketControl?.updateValueAndValidity();
  }

  // Power of 2 validator for BracketOnly format
  powerOfTwoValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value) return null;

      const isPowerOfTwo = value > 0 && (value & (value - 1)) === 0;
      return isPowerOfTwo ? null : { powerOfTwo: true };
    };
  }

  // Divisibility validator for GroupsAndBracket and GroupsOnly formats
  divisibleByValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const maxTeams = control.value;
      const teamsPerGroup = this.configForm?.get('teamsPerGroup')?.value;

      if (!maxTeams || !teamsPerGroup) {
        return null;
      }

      if (maxTeams % teamsPerGroup !== 0) {
        return { divisibleBy: true };
      }

      return null;
    };
  }

  onStepChange(event: StepperSelectionEvent): void {
    // If going backward, show confirmation
    if (event.previouslySelectedIndex > event.selectedIndex) {
      this.confirmBackNavigation(event);
    }
  }

  confirmBackNavigation(event: StepperSelectionEvent): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Go Back?',
        message: 'Are you sure you want to go back? Your current step data will be preserved.'
      }
    });

    dialogRef.afterClosed().subscribe(confirmed => {
      if (!confirmed) {
        // Cancel navigation - stay on current step
        setTimeout(() => {
          this.stepper.selectedIndex = event.previouslySelectedIndex;
        });
      }
    });
  }

  getFormatDisplayName(format: Format | null): string {
    if (!format) return '';

    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.GroupsAndBracket:
        return 'Groups + Bracket';
      case Format.GroupsOnly:
        return 'Groups Only';
      default:
        return '';
    }
  }

  submit(): void {
    if (!this.detailsForm.valid || !this.formatForm.valid || !this.configForm.valid) {
      this.markFormGroupTouched(this.detailsForm);
      this.markFormGroupTouched(this.formatForm);
      this.markFormGroupTouched(this.configForm);
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const tournament: Tournament = {
      ...this.detailsForm.value,
      ...this.formatForm.value,
      ...this.configForm.value
    };

    // For BracketOnly, ensure teamsPerBracket equals maxTeams
    if (tournament.format === Format.BracketOnly) {
      tournament.teamsPerBracket = tournament.maxTeams;
    }

    this.tournamentService.createTournament(tournament)
      .pipe(
        finalize(() => this.isSubmitting = false)
      )
      .subscribe({
        next: (res) => {
          this.router.navigate(['/tournament', res.id]);
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
