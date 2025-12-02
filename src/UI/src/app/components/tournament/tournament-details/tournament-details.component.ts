import { Component, OnInit } from '@angular/core';
import { Format, Tournament, TournamentStatus, TournamentStateDTO } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, catchError, finalize, forkJoin, of, from } from 'rxjs';
import { concatMap, switchMap, tap, map, toArray } from 'rxjs/operators';
import { Standing, StandingType } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Team } from '../../../models/team';
import { TeamService } from '../../../services/team/team.service';
import { MessageService } from 'primeng/api';
import { MatchService } from '../../../services/match/match.service';
import { MatchFinishedIds } from '../../../models/matchresult';
import { FinalStanding } from '../../../models/final-standing';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrls: ['./tournament-details.component.css']
})
export class TournamentDetailsComponent implements OnInit {
  tournament$: Observable<Tournament | null> = of(null);
  tournament: Tournament | null = null;
  tournamentId: number | null = null;
  tournamentState: TournamentStateDTO | null = null;
  bracketMatches: any[] = []; // Placeholder for bracket matches

  // Standings data
  standings: Standing[] = [];
  groups: Standing[] = [];
  brackets: Standing[] = [];

  // Team management
  allTeams: Team[] = [];

  // Final Results
  finalStandings: FinalStanding[] = [];


  // UI state
  isReloading = false;
  isAddingTeams = false;
  errorMessage = '';
  activeTabIndex = 0; // Track active tab to preserve state during reloads

  // Constants for template
  Format = Format;
  TournamentStatus = TournamentStatus;

  constructor(
    private tournamentService: TournamentService,
    private standingService: StandingService,
    private teamService: TeamService,
    private matchService: MatchService,
    private route: ActivatedRoute,
    private router: Router,
    private messageService: MessageService
  ) { }

  ngOnInit(): void {
    this.initForms();
    this.loadTournamentData();
    this.loadTournamentState();
    this.loadAllTeams();
  }

  initForms(): void {
    // Form initialization moved to team-management-card component
  }

  loadBracketMatches(): void {
    // Only load if in bracket stage
    if (this.tournamentState?.currentStatus === TournamentStatus.BracketInProgress ||
      this.tournamentState?.currentStatus === TournamentStatus.Finished) {

      // Find the bracket standing
      const bracketStanding = this.brackets.length > 0 ? this.brackets[0] : null;

      if (bracketStanding) {
        this.matchService.getMatchesByStandingId(bracketStanding.id)
          .subscribe({
            next: (matches) => {
              // Sort by round (descending) then seed (ascending) for display
              this.bracketMatches = matches.sort((a, b) => (b.round || 0) - (a.round || 0) || (a.seed || 0) - (b.seed || 0));
            },
            error: (error) => {
              console.error('Error loading bracket matches:', error);
            }
          });
      }
    }
  }

  loadTournamentData(): void {
    this.tournament$ = this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id) {
          this.tournamentId = +id;
          this.isReloading = true;
          return this.tournamentService.getTournamentWithTeams(this.tournamentId).pipe(
            tap(tournament => {
              if (tournament) {
                this.tournament = tournament;
                this.loadStandings();
              }
            }),
            catchError(error => {
              this.errorMessage = 'Error loading tournament data. Please try again.';
              console.error('Error loading tournament:', error);
              return of(null);
            }),
            finalize(() => this.isReloading = false)
          );
        } else {
          return of(null);
        }
      })
    );
  }

  loadTournamentState(): void {
    this.route.paramMap.pipe(
      switchMap(params => {
        const id = params.get('id');
        if (id) {
          this.tournamentId = +id;
          return this.tournamentService.getTournamentState(this.tournamentId);
        }
        return of(null);
      })
    ).subscribe({
      next: (state) => {
        this.tournamentState = state;
      },
      error: (error) => {
        console.error('Error loading tournament state:', error);
      }
    });
  }

  loadStandings(): void {
    if (!this.tournamentId) return;

    this.standingService.getStandingsByTournamentId(this.tournamentId).subscribe({
      next: (standings: Standing[]) => {
        this.standings = standings;
        this.groups = standings.filter(s => s.standingType === StandingType.Group);
        this.brackets = standings.filter(s => s.standingType === StandingType.Bracket);

        // Load bracket matches if in bracket stage
        this.loadBracketMatches();

        // Load final standings if tournament is finished
        if (this.tournamentState?.currentStatus === TournamentStatus.Finished) {
          this.loadFinalStandings();
        }
      },
      error: (error) => {
        console.error('Error loading standings', error);
      }
    });
  }

  loadFinalStandings(): void {
    if (!this.tournamentId) return;

    this.tournamentService.getFinalStandings(this.tournamentId).subscribe({
      next: (standings) => {
        this.finalStandings = standings;
      },
      error: (error) => {
        console.error('Error loading final standings', error);
      }
    });
  }

  onStartBracket(): void {
    if (!confirm('Start bracket? This will seed teams from group results.')) {
      return;
    }

    if (!this.tournamentId) return;

    this.isReloading = true;
    this.tournamentService.startBracket(this.tournamentId).subscribe({
      next: (response) => {
        if (response.success) {
          this.showSuccess(response.message);
          this.loadTournamentState();
          this.reloadTournamentData();
          // Delay bracket loading slightly to ensure standings are updated
          setTimeout(() => this.loadBracketMatches(), 500);
        } else {
          this.showError(response.message);
        }
      },
      error: (err) => {
        this.showError('Error starting bracket');
        console.error('Error starting bracket:', err);
      },
      complete: () => {
        this.isReloading = false;
      }
    });
  }

  loadAllTeams(): void {
    this.teamService.getAllTeams().subscribe({
      next: (teams) => {
        this.allTeams = teams;
      },
      error: (error) => {
        console.error('Error loading teams', error);
      }
    });
  }

  addBulkTeams(teamNames: string[]): void {
    if (!this.tournamentId || !this.tournament) return;

    // Validation moved to team-management-card component, but we still validate capacity here
    const remainingSlots = this.tournament.maxTeams - (this.tournament.teams?.length || 0);
    if (teamNames.length > remainingSlots) {
      this.showError(`Can only add ${remainingSlots} more teams. You're trying to add ${teamNames.length}.`);
      return;
    }

    this.isAddingTeams = true;

    // Process teams one by one and track results
    from(teamNames as string[]).pipe(
      concatMap(name => {
        // First check if team exists
        const existingTeam = this.allTeams.find(t => t.name.toLowerCase() === name.toLowerCase());

        if (existingTeam) {
          // Use existing team
          return this.teamService.addTeamToTournament(this.tournamentId!, existingTeam.id).pipe(
            map(() => ({ name, success: true, error: null })),
            catchError(error => {
              const errorMsg = 'Team name already used';
              console.error(`Error adding existing team ${name} to tournament:`, error);
              return of({ name, success: false, error: errorMsg });
            })
          );
        } else {
          // Create new team
          return this.teamService.createTeam(name).pipe(
            switchMap(createdTeam => {
              if (!createdTeam || !createdTeam.id) {
                return of({ name, success: false, error: 'Failed to create team' });
              }

              // Add newly created team to allTeams array
              this.allTeams.push({
                id: createdTeam.id,
                name: name,
                wins: 0,
                losses: 0,
                points: 0
              });

              // Add to tournament
              return this.teamService.addTeamToTournament(this.tournamentId!, createdTeam.id).pipe(
                map(() => ({ name, success: true, error: null })),
                catchError(error => {
                  const errorMsg = error?.error?.title || error?.message || 'Failed to add team to tournament';
                  console.error(`Error adding new team ${name} to tournament:`, error);
                  return of({ name, success: false, error: errorMsg });
                })
              );
            }),
            catchError(error => {
              const errorMsg = error?.error?.title || error?.message || 'Failed to create team';
              console.error(`Error creating team ${name}:`, error);
              return of({ name, success: false, error: errorMsg });
            })
          );
        }
      }),
      toArray(),
      finalize(() => {
        this.isAddingTeams = false;
      })
    ).subscribe({
      next: (results) => {
        this.reloadTournamentData();

        const succeeded = results.filter(r => r.success);
        const failed = results.filter(r => !r.success);

        if (failed.length === 0) {
          this.showSuccess(`Successfully added ${succeeded.length} team(s)`);
        } else if (succeeded.length === 0) {
          const failedList = failed.map(f => `${f.name} (${f.error})`).join(', ');
          this.showError(`Failed to add teams: ${failedList}`);
        } else {
          const failedList = failed.map(f => `${f.name} (${f.error})`).join(', ');
          this.showSuccess(`Added ${succeeded.length} team(s). Failed: ${failedList}`);
        }
      },
      error: (error) => {
        this.showError('Error adding teams');
        console.error('Error adding teams:', error);
        this.reloadTournamentData();
      }
    });
  }

  confirmRemoveTeam(team: Team): void {
    if (confirm(`Are you sure you want to remove team "${team.name}" from this tournament?`)) {
      this.removeTeam(team);
    }
  }

  removeTeam(team: Team): void {
    if (!this.tournamentId) return;

    this.teamService.removeTeamFromTournament(team.id, this.tournamentId).subscribe({
      next: () => {
        this.reloadTournamentData();
        this.showSuccess(`Removed ${team.name} from tournament`);
      },
      error: (error) => {
        this.showError('Error removing team from tournament');
        console.error('Error removing team:', error);
      }
    });
  }

  startTournament(): void {
    if (!this.tournamentId || !this.tournament) return;

    if (this.tournament.teams.length < 2) {
      this.showError('Cannot start tournament with fewer than 2 teams');
      return;
    }

    this.isReloading = true;
    this.tournamentService.startTournament(this.tournamentId).pipe(
      catchError(error => {
        this.showError('Error starting tournament');
        console.error('Error in tournament starting process:', error);
        return of(null);
      }),
      finalize(() => {
        this.isReloading = false;
      })
    ).subscribe(() => {
      this.reloadTournamentData();
      this.showSuccess('Tournament successfully started!');
    });
  }

  onStartGroups(): void {
    if (!confirm('Start group stage? Registration will be closed.')) {
      return;
    }

    if (!this.tournamentId) return;

    this.isReloading = true;
    this.tournamentService.startGroups(this.tournamentId).subscribe({
      next: (response) => {
        if (response.success) {
          this.showSuccess(response.message);
          this.loadTournamentState();
          this.reloadTournamentData();
        } else {
          this.showError(response.message);
        }
      },
      error: (err) => {
        this.showError('Error starting groups');
        console.error('Error starting groups:', err);
      },
      complete: () => {
        this.isReloading = false;
      }
    });
  }

  reloadTournamentData(): void {
    if (!this.tournamentId) return;

    this.isReloading = true;

    // Also reload tournament state to keep it in sync
    this.loadTournamentState();

    this.tournament$ = this.tournamentService.getTournamentWithTeams(this.tournamentId).pipe(
      tap(tournament => {
        if (tournament) {
          this.tournament = tournament;
          this.loadStandings();
        }
      }),
      catchError(error => {
        this.errorMessage = 'Error reloading tournament data';
        console.error('Error reloading tournament data', error);
        return of(null);
      }),
      finalize(() => {
        this.isReloading = false;
      })
    );
  }

  onGroupMatchFinished(result: any): void {
    if (!this.tournamentId) return;

    // Check if all groups are now finished
    if (result.allGroupsFinished) {
      this.showSuccess('All groups completed! You can now start the bracket.');

      // Reload tournament state to show updated status and Start Bracket button
      this.loadTournamentState();
      this.reloadTournamentData();
      return;
    }


    // Normal case: just a regular match completion, reload data
    this.reloadTournamentData();
  }

  onBracketMatchFinished(result: MatchFinishedIds): void {
    // Check if tournament finished
    if (result.tournamentFinished) {
      // Store final standings
      if (result.finalStandings) {
        this.finalStandings = result.finalStandings;
      }

      // Reload both tournament data AND state
      this.loadTournamentState();
      this.reloadTournamentData();

      // Show success message
      const champTeam = this.tournament?.teams?.find(t => t.id === result.winnerId);
      const champName = champTeam?.name || `Team ${result.winnerId}`;
      this.showSuccess(`Tournament Complete! Champion: ${champName}`);

      return;
    }

    // Regular match completion - reload data to update bracket
    this.reloadTournamentData();
  }

  // UI Helper Methods - moved to sub-components

  // Notification methods
  showSuccess(message: string): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Success',
      detail: message,
      life: 3000
    });
  }

  showError(message: string): void {
    this.messageService.add({
      severity: 'error',
      summary: 'Error',
      detail: message,
      life: 5000
    });
  }
}
