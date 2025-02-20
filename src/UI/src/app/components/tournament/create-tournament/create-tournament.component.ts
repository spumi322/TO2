import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Format, Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css'] // Correct property name!
})

export class CreateTournamentComponent implements OnInit {
  form!: FormGroup;
  Format = Format;

  constructor(private fb: FormBuilder, private tournamentService: TournamentService) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: ['', Validators.required],
      format: [null, Validators.required],
      maxTeams: [''],
      teamsPerGroup: [''],
      teamsPerBracket: ['']
    });
  }

  onFormatChange(): void {
    const format = this.form.get('format')?.value;
    if (format === Format.BracketOnly) {
      this.form.get('maxTeams')?.setValidators(Validators.required);
      this.form.get('teamsPerGroup')?.clearValidators();
      this.form.get('teamsPerBracket')?.clearValidators();
    } else if (format === Format.BracketAndGroups) {
      this.form.get('teamsPerGroup')?.setValidators(Validators.required);
      this.form.get('teamsPerBracket')?.setValidators(Validators.required);
      this.form.get('maxTeams')?.clearValidators();
    }
    this.form.get('maxTeams')?.updateValueAndValidity();
    this.form.get('teamsPerGroup')?.updateValueAndValidity();
    this.form.get('teamsPerBracket')?.updateValueAndValidity();
  }

  submit(): void {
    if (this.form.valid) {
      const newTournament: Tournament = this.form.value;

      this.tournamentService.createTournament(newTournament).subscribe({
        next: (res) => console.log('Tournament created:', res),
        error: (err) => console.error('Error creating tournament:', err)
      });
    }
  }
}
