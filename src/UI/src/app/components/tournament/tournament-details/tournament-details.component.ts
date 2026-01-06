import { Component, OnInit, OnDestroy } from '@angular/core';
import { Format, Tournament, TournamentStatus, TournamentStateDTO } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, catchError, finalize, forkJoin, of, from, Subject } from 'rxjs';
import { concatMap, switchMap, tap, map, toArray, takeUntil, debounceTime } from 'rxjs/operators';
import { Standing, StandingType } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Team } from '../../../models/team';
import { TeamService } from '../../../services/team/team.service';
import { MessageService } from 'primeng/api';
import { MatchService } from '../../../services/match/match.service';
import { MatchFinishedIds } from '../../../models/matchresult';
import { FinalStanding } from '../../../models/final-standing';
import { TournamentContextService } from '../../../services/tournament-context.service';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrls: ['./tournament-details.component.css']
})
export class TournamentDetailsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
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
    private messageService: MessageService,
    private tournamentContext: TournamentContextService,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.initForms();
    this.loadTournamentData();
    this.loadTournamentState();
    this.loadAllTeams();

    // Subscribe to action triggers from navbar
    this.tournamentContext.startGroups$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.onStartGroups());

    this.tournamentContext.startTournament$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.startTournament());

    this.tournamentContext.startBracket$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.onStartBracket());

    // Subscribe to groups/bracket from context service
    this.tournamentContext.groups$
      .pipe(takeUntil(this.destroy$))
      .subscribe(groups => {
        this.groups = groups;
      });

    this.tournamentContext.bracket$
      .pipe(takeUntil(this.destroy$))
      .subscribe(bracket => {
        this.brackets = bracket ? [bracket] : [];
      });

    // Subscribe to SignalR real-time events
    const currentUser = this.authService.getAccessToken() ? this.getCurrentUserName() : null;

    this.tournamentContext.tournamentUpdated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Tournament updated by ${event.updatedBy}`);
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.teamAdded$
      .pipe(
        debounceTime(500), // Wait 500ms after last event before reloading
        takeUntil(this.destroy$)
      )
      .subscribe((event) => {
        // Ignore events while we're in the middle of adding teams to avoid flickering
        if (event.updatedBy !== currentUser && !this.isAddingTeams) {
          this.showInfo(`Teams updated by ${event.updatedBy}`);
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.teamRemoved$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Team removed by ${event.updatedBy}`);
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.gameUpdated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        // Always handle the event (including current user's events)
        this.tournamentContext.handleGameUpdated(event);

        // Handle tournament completion
        if (event.tournamentFinished) {
          if (event.finalStandings) {
            this.finalStandings = event.finalStandings;
          }
          this.loadTournamentState();

          const champTeam = this.tournament?.teams?.find(t => t.id === event.match.winnerId);
          const champName = champTeam?.name || 'Champion';
          this.showSuccess(`Tournament Complete! Champion: ${champName}`);
        }
        // Handle all groups finished
        else if (event.allGroupsFinished) {
          this.showSuccess('All groups completed! You can now start the bracket.');
          this.loadTournamentState();
        }

        // Only show notification for other users' regular game updates
        if (event.updatedBy !== currentUser && !event.tournamentFinished && !event.allGroupsFinished) {
          this.showInfo(`Game scored by ${event.updatedBy}`);
        }
      });

    this.tournamentContext.matchUpdated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Match completed by ${event.updatedBy}`);
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.standingUpdated$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Standing updated by ${event.updatedBy}`);
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.groupsStarted$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Groups started by ${event.updatedBy}`);
          this.loadTournamentState();
          this.reloadTournamentData();
        }
      });

    this.tournamentContext.bracketStarted$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event.updatedBy !== currentUser) {
          this.showInfo(`Bracket started by ${event.updatedBy}`);
          this.loadTournamentState();
          this.reloadTournamentData();
        }
      });
  }

  private getCurrentUserName(): string | null {
    const user = this.authService.getCurrentUser();
    return user?.userName || null;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.tournamentContext.clear();
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
                this.tournamentContext.setTournament(tournament);
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
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (state) => {
        this.tournamentState = state;
        this.tournamentContext.setState(state);
      },
      error: (error) => {
        console.error('Error loading tournament state:', error);
      }
    });
  }

  loadStandings(): void {
    if (!this.tournamentId) return;

    // Load detailed groups
    this.standingService.getGroupsWithDetails(this.tournamentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (groups: Standing[]) => {
          // Publish to context service for state management
          this.tournamentContext.setGroups(groups as any);
        },
        error: (error) => {
          console.error('Error loading groups', error);
        }
      });

    // Load detailed bracket
    this.standingService.getBracketWithDetails(this.tournamentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (bracket: Standing | null) => {
          // Publish to context service for state management
          this.tournamentContext.setBracket(bracket as any);
        },
        error: (error) => {
          console.error('Error loading bracket', error);
        }
      });

    // Load bracket matches if in bracket stage
    this.loadBracketMatches();

    // Load final standings if tournament is finished
    if (this.tournamentState?.currentStatus === TournamentStatus.Finished) {
      this.loadFinalStandings();
    }
  }

  loadFinalStandings(): void {
    if (!this.tournamentId) return;

    this.tournamentService.getFinalStandings(this.tournamentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (standings) => {
          this.finalStandings = standings;
        },
        error: (error) => {
          console.error('Error loading final standings', error);
        }
      });
  }

  onStartBracket(): void {
    if (!this.tournamentId) return;

    // Different messages based on tournament format
    const confirmMessage = this.tournament?.format === Format.BracketOnly
      ? 'Start tournament? Registration will be closed and bracket will be initialized.'
      : 'Start bracket? This will seed teams from group results.';

    if (!confirm(confirmMessage)) {
      return;
    }

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
    this.teamService.getAllTeams()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
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
            map(() => ({ name, success: true, error: null, team: existingTeam })),
            catchError(error => {
              const errorMsg = 'Team name already used';
              console.error(`Error adding existing team ${name} to tournament:`, error);
              return of({ name, success: false, error: errorMsg, team: null });
            })
          );
        } else {
          // Create new team
          return this.teamService.createTeam(name).pipe(
            switchMap(createdTeam => {
              if (!createdTeam || !createdTeam.id) {
                return of({ name, success: false, error: 'Failed to create team', team: null });
              }

              // Create team object (will add to allTeams later)
              const newTeam: Team = {
                id: createdTeam.id,
                name: name,
                wins: 0,
                losses: 0,
                points: 0
              };

              // Add to tournament
              return this.teamService.addTeamToTournament(this.tournamentId!, createdTeam.id).pipe(
                map(() => ({ name, success: true, error: null, team: newTeam })),
                catchError(error => {
                  const errorMsg = error?.error?.title || error?.message || 'Failed to add team to tournament';
                  console.error(`Error adding new team ${name} to tournament:`, error);
                  return of({ name, success: false, error: errorMsg, team: null });
                })
              );
            }),
            catchError(error => {
              const errorMsg = error?.error?.title || error?.message || 'Failed to create team';
              console.error(`Error creating team ${name}:`, error);
              return of({ name, success: false, error: errorMsg, team: null });
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
        // Update local state ONCE with all successfully added teams
        const succeeded = results.filter(r => r.success);
        const failed = results.filter(r => !r.success);

        if (succeeded.length > 0 && this.tournament) {
          const addedTeams = succeeded.map(r => r.team).filter((t): t is Team => t !== null);

          // Update allTeams array ONCE with newly created teams (not already in allTeams)
          const newlyCreatedTeams = addedTeams.filter(t => !this.allTeams.some(at => at.id === t.id));
          if (newlyCreatedTeams.length > 0) {
            this.allTeams = [...this.allTeams, ...newlyCreatedTeams];
          }

          // Update tournament ONCE with ALL added teams (new + existing)
          const updatedTournament = {
            ...this.tournament,
            teams: [...this.tournament.teams, ...addedTeams]
          };
          this.tournament = updatedTournament;
          this.tournamentContext.setTournament(updatedTournament);
        }

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
      }
    });
  }

  confirmRemoveTeam(team: Team): void {
    if (confirm(`Are you sure you want to remove team "${team.name}" from this tournament?`)) {
      this.removeTeam(team);
    }
  }

  removeTeam(team: Team): void {
    if (!this.tournamentId || !this.tournament) return;

    this.teamService.removeTeamFromTournament(team.id, this.tournamentId).subscribe({
      next: () => {
        // Update local state: remove team from tournament
        if (this.tournament) {
          const updatedTournament = {
            ...this.tournament,
            teams: this.tournament.teams.filter(t => t.id !== team.id)
          };
          this.tournament = updatedTournament;
          this.tournamentContext.setTournament(updatedTournament);
        }
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

    // BracketOnly format: Call start-bracket endpoint directly
    if (this.tournament.format === Format.BracketOnly) {
      this.onStartBracket();
      return;
    }

    // GroupsOnly format: Call start-groups endpoint directly
    if (this.tournament.format === Format.GroupsOnly) {
      this.onStartGroups();
      return;
    }

    // Other formats: Just close registration
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
    if (!this.tournamentId) return;

    // Different messages based on tournament format
    const confirmMessage = this.tournament?.format === Format.GroupsOnly
      ? 'Start tournament? Registration will be closed and groups will be initialized.'
      : 'Start group stage? Registration will be closed.';

    if (!confirm(confirmMessage)) {
      return;
    }

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
          this.tournamentContext.setTournament(tournament);
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
    // No longer needed - group updates handled by SignalR
    // All groups finished also handled in gameUpdated$ subscription
  }

  onBracketMatchFinished(result: MatchFinishedIds): void {
    // No longer needed - bracket updates handled by SignalR
    // Tournament completion also handled in gameUpdated$ subscription
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

  showInfo(message: string): void {
    this.messageService.add({
      severity: 'info',
      summary: 'Update',
      detail: message,
      life: 3000
    });
  }
}
