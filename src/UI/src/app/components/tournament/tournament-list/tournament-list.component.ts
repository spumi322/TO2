import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Tournament } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Router } from '@angular/router';
import { FormatConfig } from '../../../utils/format-config';
import { StatusConfig } from '../../../utils/status-config';

interface GroupedTournaments {
  upcoming: Tournament[];
  ongoing: Tournament[];
  finished: Tournament[];
}

@Component({
  selector: 'app-tournament-list',
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css']
})
export class TournamentListComponent implements OnInit {
  groupedTournaments$!: Observable<GroupedTournaments>;

  // Expose utilities for template
  FormatConfig = FormatConfig;
  StatusConfig = StatusConfig;

  constructor(
    private tournamentService: TournamentService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // KEEP using getAllTournamentsWithTeams() - no backend changes
    this.groupedTournaments$ = this.tournamentService.getAllTournamentsWithTeams().pipe(
      map(tournaments => this.groupTournaments(tournaments))
    );
  }

  private groupTournaments(tournaments: Tournament[]): GroupedTournaments {
    return {
      upcoming: tournaments.filter(t => StatusConfig.getStatusCategory(t.status) === 'upcoming'),
      ongoing: tournaments.filter(t => StatusConfig.getStatusCategory(t.status) === 'ongoing'),
      finished: tournaments.filter(t => StatusConfig.getStatusCategory(t.status) === 'finished')
    };
  }

  goToDetails(tournamentId: number): void {
    this.router.navigate(['/tournament', tournamentId]);
  }

  trackByTournament(index: number, tournament: Tournament): number {
    return tournament.id;
  }
}
