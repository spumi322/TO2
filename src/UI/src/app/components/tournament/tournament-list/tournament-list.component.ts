import { Component } from '@angular/core';
import { Observable } from 'rxjs';
import { Tournament, TournamentStatus } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { TournamentStatusLabel } from '../tournament-status-label/tournament-status-label';
import { Router } from '@angular/router';

@Component({
  selector: 'app-tournament-list',
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css']
})
export class TournamentListComponent {
  tournaments$!: Observable<Tournament[]>;
  tournamentStatuses = Object.values(TournamentStatus).filter(v => typeof v === 'number') as TournamentStatus[];
  
  constructor(private tournamentService: TournamentService,private router: Router) { }

  ngOnInit(): void {
    this.tournaments$ = this.tournamentService.getAllTournamentsWithTeams();
    this.tournaments$.subscribe(
      tournaments => console.log('Received tournaments:', tournaments),
      error => console.error('Error fetching tournaments:', error)
    );
  }

  getStatusLabel(status: TournamentStatus): string {
    return TournamentStatusLabel.getLabel(status);
  }

  goToDetails(tournamentId: number): void {
    this.router.navigate(['/tournament', tournamentId]);
  }


}
