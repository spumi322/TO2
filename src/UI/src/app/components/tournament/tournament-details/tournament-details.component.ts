import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { Format, Tournament, TournamentStatus } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, catchError, finalize, forkJoin, of, from } from 'rxjs';
import { concatMap, switchMap, tap, map, toArray } from 'rxjs/operators';
import { Standing, StandingType } from '../../../models/standing';
import { StandingService } from '../../../services/standing/standing.service';
import { Team } from '../../../models/team';
import { TeamService } from '../../../services/team/team.service';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TournamentStatusLabel } from '../tournament-status-label/tournament-status-label';

@Component({
  selector: 'app-tournament-details',
  templateUrl: './tournament-details.component.html',
  styleUrls: ['./tournament-details.component.css']
})
export class TournamentDetailsComponent implements OnInit {
  @ViewChild('confirmDialog') confirmDialog!: TemplateRef<any>;

  tournament$: Observable<Tournament | null> = of(null);
  tournament: Tournament | null = null;
  tournamentId: number | null = null;

  // Standings data
  standings: Standing[] = [];
  groups: Standing[] = [];
  brackets: Standing[] = [];

  // Team management
  allTeams: Team[] = [];
  teamToRemove: Team | null = null;

  // Forms
  bulkAddForm!: FormGroup;

  // UI state
  isReloading = false;
  isAddingTeams = false;
  errorMessage = '';

  // Constants for template
  Format = Format;

  constructor(
    private tournamentService: TournamentService,
    private standingService: StandingService,
    private teamService: TeamService,
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) { }

  ngOnInit(): void {
    this.initForms();
    this.loadTournamentData();
    this.loadAllTeams();
  }

  initForms(): void {
    this.bulkAddForm = this.fb.group({
      teamNames: ['', Validators.required]
    });
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

  loadStandings(): void {
    if (!this.tournamentId) return;

    this.standingService.getStandingsByTournamentId(this.tournamentId).subscribe({
      next: (standings: Standing[]) => {
        this.standings = standings;
        this.groups = standings.filter(s => s.standingType === StandingType.Group);
        this.brackets = standings.filter(s => s.standingType === StandingType.Bracket);
      },
      error: (error) => {
        console.error('Error loading standings', error);
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

  addBulkTeams(): void {
    if (this.bulkAddForm.invalid || !this.tournamentId || !this.tournament) return;

    const teamNamesInput = this.bulkAddForm.get('teamNames')?.value;
    if (!teamNamesInput) return;

    const teamNames = teamNamesInput
      .split(',')
      .map((name: string) => name.trim())
      .filter((name: string) => name.length > 0);

    if (!teamNames.length) {
      this.showError('No valid team names provided');
      return;
    }

    // Check if we'll exceed max capacity
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
        this.bulkAddForm.reset();
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
    this.teamToRemove = team;

    this.dialog.open(this.confirmDialog).afterClosed().subscribe(result => {
      if (result && this.teamToRemove) {
        this.removeTeam(this.teamToRemove);
      }
      this.teamToRemove = null;
    });
  }

  removeTeam(team: Team): void {
    if (!this.tournamentId) return;

    this.tournamentService.removeTeam(team.id, this.tournamentId).subscribe({
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

    this.tournamentService.requestStartTournament(this.tournamentId).pipe(
      catchError(error => {
        const errorMessage = error?.error?.error || 'Error starting tournament';
        this.showError(errorMessage);
        console.error('Error starting tournament:', error);
        return of(null);
      }),
      finalize(() => {
        this.isReloading = false;
      })
    ).subscribe(response => {
      if (response) {
        this.showSuccess('Tournament start initiated! Processing...');

        // Poll for completion
        this.pollTournamentStatus();
      }
    });
  }

  private pollTournamentStatus(): void {
    const pollInterval = setInterval(() => {
      if (!this.tournamentId) {
        clearInterval(pollInterval);
        return;
      }

      this.tournamentService.getTournament(this.tournamentId).subscribe({
        next: (tournament) => {
          // Check if processing is complete
          if (!tournament.isProcessing) {
            clearInterval(pollInterval);

            if (tournament.status === TournamentStatus.Ongoing) {
              this.showSuccess('Tournament started successfully!');
              this.reloadTournamentData();
            } else {
              this.showError('Tournament start failed. Please try again.');
              this.reloadTournamentData();
            }
          }
        },
        error: (error) => {
          clearInterval(pollInterval);
          this.showError('Error checking tournament status');
          console.error('Polling error:', error);
        }
      });
    }, 2000); // Poll every 2 seconds

    // Safety timeout: stop polling after 30 seconds
    setTimeout(() => {
      clearInterval(pollInterval);
      this.reloadTournamentData();
    }, 30000);
  }

  reloadTournamentData(): void {
    if (!this.tournamentId) return;

    this.isReloading = true;
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

  // UI Helper Methods
  getFormatLabel(format: Format): string {
    switch (format) {
      case Format.BracketOnly:
        return 'Bracket Only';
      case Format.BracketAndGroups:
        return 'Group Stage + Bracket';
      default:
        return 'Unknown Format';
    }
  }

  getStatusClass(status: TournamentStatus): string {
    switch (status) {
      case TournamentStatus.Upcoming:
        return 'status-upcoming';
      case TournamentStatus.Ongoing:
        return 'status-ongoing';
      case TournamentStatus.Finished:
        return 'status-finished';
      case TournamentStatus.Cancelled:
        return 'status-cancelled';
      default:
        return '';
    }
  }

  getStatusLabel(status: TournamentStatus): string {
    return TournamentStatusLabel.getLabel(status);
  }

  showSuccess(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  showError(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  getTeamsPerGroup(): number | null {
    return this.groups.length > 0 ? this.groups[0].maxTeams : null;
  }

  getTeamsPerBracket(): number | null {
    return this.brackets.length > 0 ? this.brackets[0].maxTeams : null;
  }
}
