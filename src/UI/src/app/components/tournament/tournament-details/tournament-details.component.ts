import { Component, OnInit, TemplateRef, ViewChild } from '@angular/core';
import { Format, Tournament, TournamentStatus, TournamentStateDTO } from '../../../models/tournament';
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
import { FinalStanding } from '../../../models/final-standing';

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
  tournamentState: TournamentStateDTO | null = null;

  // Standings data
  standings: Standing[] = [];
  groups: Standing[] = [];
  brackets: Standing[] = [];

  // Team management
  allTeams: Team[] = [];
  teamToRemove: Team | null = null;

  // Champion and Final Standings
  champion: Team | null = null;
  finalStandings: FinalStanding[] = [];

  // Forms
  bulkAddForm!: FormGroup;

  // UI state
  isReloading = false;
  isAddingTeams = false;
  errorMessage = '';
  selectedTabIndex = 0; // Track active tab to preserve state during reloads

  // Constants for template
  Format = Format;
  TournamentStatus = TournamentStatus;

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
    this.loadTournamentState();
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

        // Load champion and final standings if tournament is finished
        if (this.tournament?.status === TournamentStatus.Finished) {
          this.loadChampionAndStandings();
        }
      },
      error: (error) => {
        console.error('Error loading standings', error);
      }
    });
  }

  loadChampionAndStandings(): void {
    if (!this.tournamentId) return;

    this.tournamentService.getChampion(this.tournamentId).subscribe({
      next: (champion) => {
        this.champion = champion;
      },
      error: (error) => {
        console.error('Error loading champion', error);
      }
    });

    this.tournamentService.getFinalStandings(this.tournamentId).subscribe({
      next: (standings) => {
        this.finalStandings = standings;
      },
      error: (error) => {
        console.error('Error loading final standings', error);
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
    this.tournamentService.startTournament(this.tournamentId).pipe(
      switchMap(() => this.generateGroupMatches()),
      switchMap((standingIds: number[]) => {
        const generateGamesRequests = standingIds.map(standingId =>
          this.standingService.generateGames(standingId)
        );

        return forkJoin(generateGamesRequests.length ? generateGamesRequests : [of(null)]);
      }),
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

  generateGroupMatches(): Observable<number[]> {
    if (!this.tournamentId) return of([]);

    return this.standingService.generateGroupMatches(this.tournamentId).pipe(
      catchError(error => {
        console.error('Error generating group matches:', error);
        return of([]);
      })
    );
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

  onGroupMatchFinished(result: any): void {
    console.log('STEP 4: Group match finished with lifecycle info:', result);

    if (!this.tournamentId) return;

    // STEP 4: Use lifecycle information from backend instead of polling
    // Backend now tells us explicitly when bracket is seeded
    if (result.allGroupsFinished && result.bracketSeeded) {
      console.log('✓ All groups finished and bracket seeded! Redirecting immediately...');
      this.showSuccess(result.bracketSeedMessage || 'Bracket seeded! Redirecting...');

      // Immediate redirect - no timeout needed!
      this.router.navigate(['/tournament', this.tournamentId, 'bracket']);
      return;
    }

    if (result.allGroupsFinished && !result.bracketSeeded) {
      // Rare case: groups finished but seeding failed
      console.warn('Groups finished but bracket seeding failed:', result.bracketSeedMessage);
      this.showError(result.bracketSeedMessage || 'Bracket seeding failed');
    }

    // Normal case: just a regular match completion, reload data
    console.log('Regular match completion, reloading tournament data...');
    this.reloadTournamentData();
  }

  onBracketMatchFinished(result: any): void {
    // Reload tournament data to get latest bracket status
    this.reloadTournamentData();
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
      case TournamentStatus.Setup:
        return 'status-setup';
      case TournamentStatus.SeedingGroups:
      case TournamentStatus.SeedingBracket:
        return 'status-seeding';
      case TournamentStatus.GroupsInProgress:
      case TournamentStatus.GroupsCompleted:
      case TournamentStatus.BracketInProgress:
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
