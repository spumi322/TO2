import { Component, OnInit, OnDestroy } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { map, takeUntil } from 'rxjs/operators';
import { Router } from '@angular/router';

import { TournamentList } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { Format, TournamentStatus } from '../../../models/tournament';
import { TournamentContextService } from '../../../services/tournament-context.service';
import { AuthService } from '../../../services/auth/auth.service';

interface GroupedTournaments {
  upcoming: TournamentList[];
  ongoing: TournamentList[];
  finished: TournamentList[];
}

@Component({
  selector: 'app-tournament-list',
  templateUrl: './tournament-list.component.html',
  styleUrls: ['./tournament-list.component.css']
})
export class TournamentListComponent implements OnInit, OnDestroy {
  groupedTournaments$!: Observable<GroupedTournaments>;
  private destroy$ = new Subject<void>();

  constructor(
    private tournamentService: TournamentService,
    private router: Router,
    private tournamentContext: TournamentContextService,
    private authService: AuthService
  ) { }

  async ngOnInit(): Promise<void> {
    this.loadTournaments();

    // Ensure SignalR is connected (don't wait for tournament to be opened)
    // Pass null to trigger connection without joining a specific tournament
    this.tournamentContext.setTournament(null);

    // Subscribe to tournament events
    const currentUser = this.authService.getCurrentUser();

    this.tournamentContext.tournamentCreated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser?.userName) {
          console.log(`[TournamentList] Tournament created by ${event.updatedBy}, reloading...`);
          this.loadTournaments();
        }
      });

    this.tournamentContext.tournamentUpdated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser?.userName) {
          console.log(`[TournamentList] Tournament updated by ${event.updatedBy}, reloading...`);
          this.loadTournaments();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadTournaments(): void {
    this.groupedTournaments$ = this.tournamentService.getTournamentList().pipe(
      map(tournaments => this.groupTournaments(tournaments))
    );
  }

  private groupTournaments(tournaments: TournamentList[]): GroupedTournaments {
    return {
      upcoming: tournaments.filter(t => this.getStatusCategory(t.status) === 'upcoming'),
      ongoing: tournaments.filter(t => this.getStatusCategory(t.status) === 'ongoing'),
      finished: tournaments.filter(t => this.getStatusCategory(t.status) === 'finished')
    };
  }

  // Status → high-level category for grouping
  private getStatusCategory(status: TournamentStatus): 'upcoming' | 'ongoing' | 'finished' {
    switch (status) {
      case TournamentStatus.Setup:
        return 'upcoming';

      case TournamentStatus.SeedingGroups:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.SeedingBracket:
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.BracketInProgress:
        return 'ongoing';

      case TournamentStatus.Finished:
      case TournamentStatus.Cancelled:
      default:
        return 'finished';
    }
  }

  // Simple status label map
  private readonly statusLabels: Record<TournamentStatus, string> = {
    [TournamentStatus.Setup]: 'Setup',
    [TournamentStatus.SeedingGroups]: 'Seeding Groups',
    [TournamentStatus.GroupsInProgress]: 'Groups In Progress',
    [TournamentStatus.GroupsCompleted]: 'Groups Completed',
    [TournamentStatus.SeedingBracket]: 'Seeding Bracket',
    [TournamentStatus.BracketInProgress]: 'Bracket In Progress',
    [TournamentStatus.Finished]: 'Finished',
    [TournamentStatus.Cancelled]: 'Cancelled'
  };

  // Local format label helper
  getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.GroupsOnly:
        return 'Groups Only';
      case Format.GroupsAndBracket:
        return 'Groups + Bracket';
      default:
        return 'Unknown Format';
    }
  }

  // Local status label helper
  getStatusLabel(status: TournamentStatus): string {
    return this.statusLabels[status] ?? 'Unknown Status';
  }

  goToDetails(tournamentId: number): void {
    this.router.navigate(['/tournament', tournamentId]);
  }

  trackByTournament(index: number, tournament: TournamentList): number {
    return tournament.id;
  }
}
