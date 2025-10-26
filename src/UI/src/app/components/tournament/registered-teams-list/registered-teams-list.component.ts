import { Component, Input, Output, EventEmitter } from '@angular/core';
import { Team } from '../../../models/team';

@Component({
  selector: 'app-registered-teams-list',
  templateUrl: './registered-teams-list.component.html',
  styleUrls: ['./registered-teams-list.component.css']
})
export class RegisteredTeamsListComponent {
  @Input() teams: Team[] = [];
  @Input() isRegistrationOpen: boolean = false;

  @Output() removeTeam = new EventEmitter<Team>();

  onRemoveTeam(team: Team): void {
    this.removeTeam.emit(team);
  }
}
