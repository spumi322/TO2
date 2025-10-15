import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Tournament, TournamentStatus } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Router } from '@angular/router';

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

  constructor(
    private tournamentService: TournamentService,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.groupedTournaments$ = this.tournamentService.getAllTournamentsWithTeams().pipe(
      map(tournaments => ({
        upcoming: tournaments.filter(t => t.status === TournamentStatus.Setup),
        ongoing: tournaments.filter(t =>
          t.status === TournamentStatus.SeedingGroups ||
          t.status === TournamentStatus.GroupsInProgress ||
          t.status === TournamentStatus.GroupsCompleted ||
          t.status === TournamentStatus.SeedingBracket ||
          t.status === TournamentStatus.BracketInProgress
        ),
        finished: tournaments.filter(t =>
          t.status === TournamentStatus.Finished ||
          t.status === TournamentStatus.Cancelled
        )
      }))
    );
  }

  // Default action when clicking on the card
  goToDetails(tournamentId: number): void {
    this.router.navigate(['/tournament', tournamentId]);
  }

  // Determines the label of the action button based on tournament status
  getActionLabel(tournament: Tournament): string {
    switch (tournament.status) {
      case TournamentStatus.Setup:
        return 'Edit';
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.SeedingBracket:
      case TournamentStatus.BracketInProgress:
        return 'Play';
      case TournamentStatus.Finished:
      case TournamentStatus.Cancelled:
        return 'Results';
      default:
        return 'Details';
    }
  }

  // Handles the action button click. Stops event propagation so the card click isn't triggered.
  action(tournament: Tournament, event: Event): void {
    event.stopPropagation();
    switch (tournament.status) {
      case TournamentStatus.Setup:
        // Navigate to the edit tournament component.
        this.router.navigate(['/tournament/edit', tournament.id]);
        break;
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.SeedingBracket:
      case TournamentStatus.BracketInProgress:
        // Navigate to a tournament details view where you can enter match results.
        this.router.navigate(['/tournament/play', tournament.id]);
        break;
      case TournamentStatus.Finished:
      case TournamentStatus.Cancelled:
        // Navigate to the tournament results view.
        this.router.navigate(['/tournament/results', tournament.id]);
        break;
      default:
        // Fallback action.
        this.router.navigate(['/tournament', tournament.id]);
        break;
    }
  }

  // trackBy function for performance in ngFor.
  trackByTournament(index: number, tournament: Tournament): number {
    return tournament.id;
  }
}
