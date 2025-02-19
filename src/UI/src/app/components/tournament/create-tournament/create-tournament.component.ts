import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrl: './create-tournament.component.css'
})
export class CreateTournamentComponent {
  tournamentForm!: FormGroup;
  formatOptions = [
    { label: 'Bracket Only', value: Format.BracketOnly },
    { label: 'Bracket and Groups', value: Format.BracketAndGroups }
  ]

  constructor(private tournamentService: TournamentService,
    private fb: FormBuilder,
    private router: Router) {
    this.tournamentForm = this.fb.group({
      name: [''],
      description: [''],
      maxTeams: [''],
      format: [''],
      teamsPerGroup: [''],
      teamsPerBracket: ['']
    });
  }

  //validate
  //(control: any) {
  //  const startDate = this.tournamentForm.get('startDate')?.value;
  //  const endDate = control.value;
  //  return startDate < endDate ? null : { invalidEndDate: true };
  //}

  onSubmit() {
    if (this.tournamentForm.valid) {
      const newTournament: Tournament = this.tournamentForm.value;

      this.tournamentService.createTournament(newTournament).subscribe(
        response => {
          console.log('Tournament created:', response);

          this.router.navigate([`/tournament/${response.id}`])
        },
        error => {
          console.error('Error creating tournament:', error);
        }
      );
    } else {
      console.error('Form is invalid');
    }
  }
}
