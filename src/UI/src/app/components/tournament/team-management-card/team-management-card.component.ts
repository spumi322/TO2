import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Tournament } from '../../../models/tournament';

@Component({
  selector: 'app-team-management-card',
  templateUrl: './team-management-card.component.html',
  styleUrls: ['./team-management-card.component.css']
})
export class TeamManagementCardComponent implements OnInit {
  @Input() tournament: Tournament | null = null;
  @Input() isAddingTeams: boolean = false;

  @Output() addTeams = new EventEmitter<string[]>();

  bulkAddForm!: FormGroup;

  constructor(private fb: FormBuilder) { }

  ngOnInit(): void {
    this.bulkAddForm = this.fb.group({
      teamNames: ['', Validators.required]
    });
  }

  onSubmit(): void {
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
}
