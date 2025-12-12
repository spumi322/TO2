import { Component, EventEmitter, Input, OnInit, OnDestroy, Output } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessageService } from 'primeng/api';
import { Match } from '../../models/match';
import { Team } from '../../models/team';
import { MatchService } from '../../services/match/match.service';
import { MatchFinishedIds, MatchResult } from '../../models/matchresult';
import { Game } from '../../models/game';

@Component({
  selector: 'app-matches',
  templateUrl: './matches.component.html',
  styleUrls: ['./matches.component.css'] 
})
export class MatchesComponent implements OnInit, OnDestroy {
  @Input() matches: Match[] = [];
  @Input() teams: Team[] = [];
  @Input() isGroupFinished: boolean = false;
  @Input() tournamentId!: number;
  @Output() matchFinished = new EventEmitter<MatchFinishedIds>();

  isUpdating: { [key: number]: boolean } = {};
  private destroy$ = new Subject<void>();

  constructor(
    private matchService: MatchService,
    private messageService: MessageService
  ) { }

  ngOnInit() {
    // Backend provides complete match data with teamAWins/teamBWins
    // No need to fetch games on init
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getTeamName(teamId: number): string {
    const team = this.teams.find(t => t.id === teamId);
    return team ? team.name : 'TBD';
  }

  updateMatchScore(matchId: number, gameWinnerId: number): void {
    const match = this.matches.find(m => m.id === matchId);
    if (!match || match.winnerId) return;

    // Find unscored game locally from match.games array
    const gameToUpdate = match.games?.find(game => !game.winnerId);
    if (!gameToUpdate) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Game Available',
        detail: 'No unscored games found for this match.'
      });
      return;
    }

    // Store previous state for rollback
    const previousTeamAWins = match.teamAWins;
    const previousTeamBWins = match.teamBWins;

    // Optimistic update
    this.isUpdating[matchId] = true;
    if (gameWinnerId === match.teamAId) {
      match.teamAWins++;
    } else if (gameWinnerId === match.teamBId) {
      match.teamBWins++;
    }

    // Single API call
    const gameResult = {
      gameId: gameToUpdate.id,
      winnerId: gameWinnerId,
      teamAScore: undefined,
      teamBScore: undefined,
      matchId: matchId,
      standingId: match.standingId,
      tournamentId: this.tournamentId
    };

    this.matchService.setGameResult(gameResult)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          // Update match completion state from backend response
          if (result.matchFinished && result.matchWinnerId && result.matchLoserId) {
            match.winnerId = result.matchWinnerId;
            match.loserId = result.matchLoserId;

            const matchFinishedData: MatchFinishedIds = {
              winnerId: result.matchWinnerId,
              loserId: result.matchLoserId,
              allGroupsFinished: result.allGroupsFinished
            };
            this.matchFinished.emit(matchFinishedData);
          }

          // Update game winner in local games array
          gameToUpdate.winnerId = gameWinnerId;
          this.isUpdating[matchId] = false;
        },
        error: (err) => {
          // Rollback optimistic update
          match.teamAWins = previousTeamAWins;
          match.teamBWins = previousTeamBWins;
          this.isUpdating[matchId] = false;

          this.messageService.add({
            severity: 'error',
            summary: 'Update Failed',
            detail: err.error?.message || 'Failed to update match score. Please try again.'
          });
        }
      });
  }
}
