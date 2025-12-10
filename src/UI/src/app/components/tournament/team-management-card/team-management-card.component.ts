import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { Subject } from 'rxjs';
import { Tournament } from '../../../models/tournament';

@Component({
  selector: 'app-team-management-card',
  templateUrl: './team-management-card.component.html',
  styleUrls: ['./team-management-card.component.css']
})
export class TeamManagementCardComponent implements OnInit, OnDestroy {
  @Input() tournament: Tournament | null = null;
  @Input() isAddingTeams: boolean = false;

  @Output() addTeams = new EventEmitter<string[]>();

  bulkAddForm!: FormGroup;
  submitted = false;
  private destroy$ = new Subject<void>();

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.bulkAddForm = this.fb.group({
      teamNames: ['', [
        Validators.required,
        this.duplicateTeamNamesValidator()
      ]]
    });

    // Reset submitted flag when user starts typing after a failed submission
    this.bulkAddForm.get('teamNames')?.valueChanges.subscribe(() => {
      if (this.submitted && this.bulkAddForm.valid) {
        this.submitted = false;  // Clear errors when form becomes valid
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onSubmit(): void {
    this.submitted = true;

    if (this.bulkAddForm.invalid || !this.tournament) return;

    const teamNamesInput = this.bulkAddForm.get('teamNames')?.value;
    if (!teamNamesInput) return;

    const teamNames = teamNamesInput
      .split(',')
      .map((name: string) => name.trim())
      .filter((name: string) => name.length > 0);

    if (!teamNames.length) {
      return;
    }

    // Check if we'll exceed max capacity
    const remainingSlots = this.tournament.maxTeams - (this.tournament.teams?.length || 0);
    if (teamNames.length > remainingSlots) {
      return; // Let parent handle error display
    }

    this.addTeams.emit(teamNames);
    this.bulkAddForm.reset();
  }

  getProgressPercentage(): number {
    if (!this.tournament) return 0;
    return ((this.tournament?.teams?.length || 0) / (this.tournament?.maxTeams || 1)) * 100;
  }

  duplicateTeamNamesValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;

      if (!value || value.trim() === '') {
        return null;  // Let required validator handle empty
      }

      // Parse team names
      const teamNames = value.split(',')
        .map((name: string) => name.trim())
        .filter((name: string) => name.length > 0)
        .map((name: string) => name.toLowerCase());

      // Check for internal duplicates
      const uniqueNames = new Set(teamNames);
      if (uniqueNames.size !== teamNames.length) {
        // Find which names are duplicated
        const duplicates = teamNames.filter((name: string, index: number) =>
          teamNames.indexOf(name) !== index
        );
        const uniqueDuplicates = [...new Set(duplicates)];

        return {
          duplicateNames: {
            value: uniqueDuplicates.join(', ')
          }
        };
      }

      // Check for names already in tournament (if tournament prop available)
      if (this.tournament?.teams) {
        const existingTeamNames = this.tournament.teams.map(t => t.name.toLowerCase());
        const alreadyInTournament = teamNames.filter((name: string) =>
          existingTeamNames.includes(name)
        );

        if (alreadyInTournament.length > 0) {
          return {
            alreadyInTournament: {
              value: alreadyInTournament.join(', ')
            }
          };
        }
      }

      // Check capacity
      if (this.tournament) {
        const remainingSlots = this.tournament.maxTeams - (this.tournament.teams?.length || 0);
        if (teamNames.length > remainingSlots) {
          return {
            capacityExceeded: {
              trying: teamNames.length,
              available: remainingSlots
            }
          };
        }
      }

      return null;
    };
  }
}
