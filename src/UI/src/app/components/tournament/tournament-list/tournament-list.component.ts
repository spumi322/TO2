import { Component } from '@angular/core';
import { Observable } from 'rxjs';
import { Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';

@Component({
  selector: 'app-tournament-list',
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css']
})
export class TournamentListComponent {
  tournaments$: Observable<Tournament[]>; 

  constructor(private tournamentService: TournamentService) {
    this.tournaments$ = this.tournamentService.getAllTournaments();
  }
}
