import { Component, Input, OnInit } from '@angular/core';
import { Match } from '../../models/match';
import { MatchService } from '../../services/match/match.service';
import { Observable, catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { Game } from '../../models/game';
import { Team } from '../../models/team';

@Component({
  selector: 'app-matches',
  templateUrl: './matches.component.html',
  styleUrl: './matches.component.css'
})
export class MatchesComponent implements OnInit {
  @Input() matches: Match[] = [];
  @Input() teams: Team[] = [];

  ngOnInit() {
    console.log(this.matches);
  }

  getTeamName(teamId: number): string {
    const team = this.teams.find(t => t.id === teamId);
    return team ? team.name : 'TBD';
  }
}

  

