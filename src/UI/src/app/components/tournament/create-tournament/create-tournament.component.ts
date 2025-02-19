import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css']
})
export class CreateTournamentComponent {
  basicInfoGroup: FormGroup;
  settingsGroup: FormGroup;
  scheduleGroup: FormGroup;

  formatOptions = [
    { value: 'Bracket', label: 'Bracket' },
    { value: 'Group', label: 'Group' },
    { value: 'BracketAndGroup', label: 'Bracket and Group' }
  ];

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    this.basicInfoGroup = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(4)]],
      description: ['']
    });

    this.settingsGroup = this.fb.group({
      maxTeams: ['', Validators.required],
      format: ['', Validators.required]
    });

    this.scheduleGroup = this.fb.group({
      startDate: ['', Validators.required]
    });
  }

  onSubmit() {
    const tournamentData = {
      name: this.basicInfoGroup.get('name')?.value,
      description: this.basicInfoGroup.get('description')?.value,
      maxTeams: this.settingsGroup.get('maxTeams')?.value,
      format: this.settingsGroup.get('format')?.value,
      teamsPerGroup: this.settingsGroup.get('format')?.value === 'BracketAndGroup' ? 4 : null,
      teamsPerBracket: this.settingsGroup.get('format')?.value === 'BracketAndGroup' ? 8 : null
    };

    this.http.post('your-backend-api-url/create-tournament', tournamentData)
      .subscribe(
        (response) => {
          console.log('Tournament created successfully:', response);
          this.router.navigate(['/tournaments']);
        },
        (error) => {
          console.error('Error creating tournament:', error);
        }
      );
  }
}
